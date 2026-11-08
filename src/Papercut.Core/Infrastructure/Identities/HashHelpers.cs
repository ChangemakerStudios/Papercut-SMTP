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


using System.Security.Cryptography;

namespace Papercut.Core.Infrastructure.Identities;

public static class HashHelpers
{
    public static string GenerateUniqueId(string input)
    {
        using SHA256 sha256 = SHA256.Create();

        // Compute the hash of the input string
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Convert the byte array to a hexadecimal string
        StringBuilder hashString = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            hashString.Append(b.ToString("x2")); // Convert to hex
        }

        return hashString.ToString();
    }
}