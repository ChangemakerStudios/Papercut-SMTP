namespace Papercut.Core.Rules.Implementations
{
    using System;

    using Papercut.Core.Helper;

    [Serializable]
    public class ConditionalForwardRule : ForwardRule
    {
        string _regexBodyMatch;

        string _regexHeaderMatch;

        public string RegexHeaderMatch
        {
            get { return _regexHeaderMatch; }
            set
            {
                if (value == _regexHeaderMatch)
                    return;
                _regexHeaderMatch = value.IsSet() && value.IsValidRegex() ? value : null; ;
                OnPropertyChanged(nameof(RegexHeaderMatch));
            }
        }

        public string RegexBodyMatch
        {
            get { return _regexBodyMatch; }
            set
            {
                if (value == _regexBodyMatch)
                    return;

                _regexBodyMatch = value.IsSet() && value.IsValidRegex() ? value : null;
                OnPropertyChanged(nameof(RegexBodyMatch));
            }
        }

        public override string Type => "Conditional Forward";

        public override string ToString()
        {
            return $"{base.ToString()}\r\nRegex Header Match: {RegexHeaderMatch}\r\nRegex Body Match: {RegexBodyMatch}";
        }
    }
}