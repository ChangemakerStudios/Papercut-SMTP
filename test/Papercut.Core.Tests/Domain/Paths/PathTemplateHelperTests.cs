// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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
using NUnit.Framework;
using Papercut.Core.Domain.Paths;

namespace Papercut.Core.Tests.Domain.Paths;

[TestFixture]
public class PathTemplateHelperTests
{
    [Test]
    public void RenderPathTemplate_WithBackslashes_NormalizesToOsSeparator()
    {
        // Arrange
        string template = "%BaseDirectory%\\Incoming";

        // Act
        string result = PathTemplateHelper.RenderPathTemplate(template);

        // Assert
        // On Linux, backslashes should be converted to forward slashes
        // On Windows, backslashes stay as backslashes (which is correct)
        if (Path.DirectorySeparatorChar == '/')
        {
            result.Should().NotContain("\\", "backslashes should be converted to forward slashes on Linux");
            result.Should().EndWith("/Incoming");
        }
        else
        {
            result.Should().EndWith("\\Incoming");
        }
    }

    [Test]
    public void RenderPathTemplate_WithForwardSlashes_NormalizesToOsSeparator()
    {
        // Arrange
        string template = "%BaseDirectory%/Incoming";

        // Act
        string result = PathTemplateHelper.RenderPathTemplate(template);

        // Assert
        result.Should().EndWith($"{Path.DirectorySeparatorChar}Incoming");
    }

    [Test]
    public void RenderPathTemplate_WithMixedSeparators_NormalizesToOsSeparator()
    {
        // Arrange
        string template = "%BaseDirectory%\\Incoming/Subfolder";

        // Act
        string result = PathTemplateHelper.RenderPathTemplate(template);

        // Assert
        // Should normalize all separators to the OS-appropriate one
        result.Should().Contain($"{Path.DirectorySeparatorChar}Incoming{Path.DirectorySeparatorChar}Subfolder");

        // Verify no mixed separators remain
        if (Path.DirectorySeparatorChar == '/')
        {
            result.Should().NotContain("\\", "backslashes should be converted to forward slashes on Linux");
        }
        else
        {
            result.Should().NotContain("/", "forward slashes should be converted to backslashes on Windows");
        }
    }

    [Test]
    public void RenderPathTemplate_WithDoubleSeparators_RemovesDuplicates()
    {
        // Arrange - simulate a scenario that might create double separators
        string template = "%BaseDirectory%\\\\Incoming";

        // Act
        string result = PathTemplateHelper.RenderPathTemplate(template);

        // Assert
        string doubleSeparator = new string(Path.DirectorySeparatorChar, 2);
        result.Should().NotContain(doubleSeparator, "duplicate separators should be removed");
    }

    [Test]
    public void RenderPathTemplate_WithBaseDirectory_ReplacesTemplate()
    {
        // Arrange
        string template = "%BaseDirectory%\\Incoming";

        // Act
        string result = PathTemplateHelper.RenderPathTemplate(template);

        // Assert
        result.Should().NotContain("%BaseDirectory%", "template variable should be replaced");
        result.Should().NotBeEmpty();
        Path.IsPathFullyQualified(result).Should().BeTrue("result should be a fully qualified path");
    }

    [Test]
    public void RenderPathTemplate_WithMultiplePathSegments_NormalizesAll()
    {
        // Arrange
        string template = "%BaseDirectory%\\Logs\\Archive\\2024";

        // Act
        string result = PathTemplateHelper.RenderPathTemplate(template);

        // Assert
        var expectedSeparator = Path.DirectorySeparatorChar.ToString();
        result.Should().Contain($"{expectedSeparator}Logs{expectedSeparator}Archive{expectedSeparator}2024");
    }

    [Test]
    public void RenderPathTemplate_OnLinux_ProducesLinuxStylePaths()
    {
        // Arrange
        string template = "%BaseDirectory%\\Incoming";

        // Act
        string result = PathTemplateHelper.RenderPathTemplate(template);

        // Assert
        if (Path.DirectorySeparatorChar == '/')
        {
            result.Should().NotContain("\\", "Linux paths should not contain backslashes");
            result.Should().EndWith("/Incoming");
        }
        else
        {
            // On Windows, this test just verifies normalization works
            result.Should().EndWith("\\Incoming");
        }
    }

    [Test]
    public void RenderPathTemplate_WithDataDirectory_ReplacesAndNormalizes()
    {
        // Arrange
        string template = "%DataDirectory%\\Logs";

        // Act
        string result = PathTemplateHelper.RenderPathTemplate(template);

        // Assert
        result.Should().NotContain("%DataDirectory%", "template variable should be replaced");
        result.Should().EndWith($"{Path.DirectorySeparatorChar}Logs");
    }

    [Test]
    public void RenderPathTemplate_WithUncPath_PreservesUncPrefix()
    {
        // Arrange
        string template = @"\\server\share\Incoming";

        // Act
        string result = PathTemplateHelper.RenderPathTemplate(template);

        // Assert
        result.Should().StartWith(@"\\", "UNC path prefix should be preserved");
    }
}
