// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


namespace Papercut.Core.Domain.Message;

public class MessageEntryDto
{
    public DateTime ModifiedDate { get; init; }

    public required string File { get; init; }

    public string? Name { get; init; }

    public string? FileSize { get; init; }

    public string? DisplayText { get; init; }

    public MessageEntry ToEntry() => new(File);
}