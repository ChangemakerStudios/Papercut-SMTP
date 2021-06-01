// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.Services
{
    using System.Windows.Media;

    using Papercut.Common.Helper;

    public class ThemeColor
    {
        public ThemeColor(string colorName, Color color)
        {
            this.Name = colorName;
            this.Color = color;
        }

        public string Name { get; }

        public string Description => this.Name.CamelCaseToSeparated();

        public Color Color { get; }

        protected bool Equals(ThemeColor other)
        {
            return this.Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((ThemeColor)obj);
        }

        public override int GetHashCode()
        {
            return this.Name != null ? this.Name.GetHashCode() : 0;
        }

        public static bool operator ==(ThemeColor left, ThemeColor right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ThemeColor left, ThemeColor right)
        {
            return !Equals(left, right);
        }
    }
}