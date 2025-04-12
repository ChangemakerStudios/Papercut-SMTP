﻿// Papercut
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


using System.Text.RegularExpressions;

namespace Papercut.Common.Helper;

public static class RegexHelpers
{
    public static bool IsValidRegex(this string regexString)
    {
        if (!regexString.IsSet())
            return false;

        try
        {
            var regex = new Regex(regexString);
            return true;
        }
        catch (ArgumentException)
        {
        }

        return false;
    }
}