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
                OnPropertyChanged("RegexHeaderMatch");
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
                OnPropertyChanged("RegexBodyMatch");
            }
        }

        public override string Type
        {
            get { return "Conditional Forward"; }
        }

        public override string ToString()
        {
            return string.Format("{0}\r\nRegex Header Match: {1}\r\nRegex Body Match: {2}",
                base.ToString(),
                RegexHeaderMatch,
                RegexBodyMatch);
        }
    }
}