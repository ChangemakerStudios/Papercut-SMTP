// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using AwesomeAssertions;
using MimeKit;
using Moq;
using NUnit.Framework;
using Papercut.Core.Domain.Message;
using Papercut.Message;
using Papercut.Message.Helpers;
using Serilog;

namespace Papercut.Message.Tests;

// #379: concurrent forwards produced corrupted copies
[TestFixture]
public class MimeMessageLoaderCloneTests
{
    private string _file = null!;

    private byte[] _attachment = null!;

    private MimeMessageLoader _loader = null!;

    [SetUp]
    public void SetUp()
    {
        _attachment = new byte[512 * 1024];
        new Random(379).NextBytes(_attachment);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Recipient", "recipient@example.com"));
        message.Subject = "Concurrent forward";

        var body = new BodyBuilder { TextBody = string.Join("\n", Enumerable.Range(0, 2000).Select(i => $"line {i}")) };
        body.Attachments.Add("data.bin", _attachment);
        message.Body = body.ToMessageBody();

        _file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.eml");
        message.WriteTo(_file);

        var repository = new Mock<IMessageRepository>();
        repository.Setup(r => r.GetMessage(It.IsAny<string?>()))
            .Returns((string? f) => Task.FromResult(File.ReadAllBytes(f!)));

        _loader = new MimeMessageLoader(repository.Object, Serilog.Core.Logger.None);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _loader.DisposeAsync();
        File.Delete(_file);
    }

    [Test]
    public async Task GetClonedAsync_Concurrently_EveryCloneIsIntact()
    {
        var entry = new MessageEntry(_file);

        // prime the shared cache, as the web UI or SignalR handler would
        await _loader.GetAsync(entry);

        int threw = 0, corrupted = 0;

        for (var round = 0; round < 10; round++)
        {
            var clones = await Task.WhenAll(
                Enumerable.Range(0, 16).Select(_ => Task.Run(async () =>
                {
                    try
                    {
                        return await _loader.GetClonedAsync(entry);
                    }
                    catch
                    {
                        Interlocked.Increment(ref threw);
                        return null;
                    }
                })));

            corrupted += clones.Count(c => c != null && !IsIntact(c));
        }

        (threw + corrupted).Should().Be(0, $"{threw} clones threw and {corrupted} were corrupted");
    }

    [Test]
    public async Task GetClonedAsync_ReturnsIntactPrivateCopies()
    {
        var entry = new MessageEntry(_file);

        var cached = await _loader.GetAsync(entry);
        var first = await _loader.GetClonedAsync(entry);
        var second = await _loader.GetClonedAsync(entry);

        IsIntact(first).Should().BeTrue();
        IsIntact(second).Should().BeTrue();

        first.Should().NotBeSameAs(cached);
        second.Should().NotBeSameAs(cached).And.NotBeSameAs(first);

        // rules rewrite recipients; that must not leak into the cache or other copies
        first.Subject = "changed";
        first.To.Clear();

        cached!.Subject.Should().Be("Concurrent forward");
        cached.To.Count.Should().Be(1);
        second.Subject.Should().Be("Concurrent forward");
        second.To.Count.Should().Be(1);
    }

    private bool IsIntact(MimeMessage? clone)
    {
        if (clone?.Attachments.SingleOrDefault() is not MimePart part) return false;

        using var decoded = new MemoryStream();
        part.Content.DecodeTo(decoded);

        return decoded.ToArray().AsSpan().SequenceEqual(_attachment)
               && clone.TextBody?.TrimEnd().EndsWith("line 1999") == true;
    }
}
