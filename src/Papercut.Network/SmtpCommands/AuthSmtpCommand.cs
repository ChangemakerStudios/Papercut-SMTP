namespace Papercut.Network.SmtpCommands
{
    using System;
    using System.Collections.Generic;

    using Papercut.Network.Protocols;

    public class AuthSmtpCommand : BaseSmtpCommand
    {
        protected override IEnumerable<string> GetMatchCommands()
        {
            return new[] { "AUTH" };
        }

        protected override void Run(string command, params string[] args)
        {
            Task result = null;

            Session.Username = null;

            // TODO: If we were interested in TLS and/or STARTTLS, we would probably
            // check here that we were in an encrypted context.  I imagine this to be
            // something like:
            //
            // if (Connection.RequiresTls && !Connection.IsSecure)
            // {
            //     result = Connection.SendLine("538 Encryption required for AUTH");
            // }
            // else...

            if ((args?.Length ?? 0) != 0)
            {
                try
                {
                    switch (args[0]?.ToUpperInvariant())
                    {
                        case "PLAIN":
                            RunAuthPlain(args);
                            break;

                        case "LOGIN":
                            RunAuthLogin();
                            break;

                        default:
                            result = Connection.SendLine("504 Authentication method not implemented");
                            break;
                    }
                }
                finally
                {
                    if (result == null)
                    {
                        result = (!string.IsNullOrEmpty(Session?.Username)
                               ? Connection.SendLine("235 Authentication successful")
                               : Connection.SendLine("535 Authentication failed"));
                    }
                }
            }
            else result = Connection.SendLine("501 Authentication method not provided");

            result.Wait();    
        }

        private void RunAuthPlain(string[] args)
        {
            // AUTH PLAIN accepts a base64-encoded authentication token comprised of
            // an authorisation value, username and password, separated by the NUL char.
            // The token is supplied either on the same line as the AUTH PLAIN command
            // or on a separate line, after being prompted by a 334 message

            string authToken = null;

            if (args.Length != 1)
            {
                // auth token is sent on same line as AUTH PLAIN
                authToken = args[1];
            }
            else
            {
                // auth token is to be sent separately on the next line
                authToken = Connection.Client.ReadTextStream(reader => {
                    Connection.SendLine("334").Wait();
                    return reader.ReadLine();
                });
            }

            if (!string.IsNullOrEmpty(authToken))
            {
                // decode the token as Base64, then split on \0 (nul);         
                var decodedToken = DecodeString(authToken)?.Split('\0');

                // [0] is the authorisation name (a SASL thing we probably don't need)
                // [1] is the username; [2] is the password
                if ((decodedToken?.Length ?? 0) == 3)
                {
                    Authenticate(decodedToken[1], decodedToken[2]);
                }
            }
        }

        private void RunAuthLogin()
        {
            // AUTH LOGIN prompts for two base64-encoded strings -- the username
            // and password -- by sending two consecutive 334 messages.  As a foible,
            // the two prompts are also base64-encoded.  We pre-encode these for some
            // tiny micro-optimisation

            const string usernamePrompt = "334 VXNlcm5hbWU6",   // Username:
                         passwordPrompt = "334 UGFzc3dvcmQ6";   // Password:

            var authToken = Connection.Client.ReadTextStream(reader => {
                var lines = new string[2];

                Connection.SendLine(usernamePrompt).Wait();
                lines[0] = reader.ReadLine();

                Connection.SendLine(passwordPrompt).Wait();
                lines[1] = reader.ReadLine();

                return lines;
            });

            if ((authToken?.Length ?? 0) == 2)
            {
                authToken[0] = DecodeString(authToken[0]);
                authToken[1] = DecodeString(authToken[1]);
                Authenticate(authToken[0], authToken[1]);
            }
        }

        private string DecodeString(string base64String)
        {
            return (base64String == null)
                 ? default(string)
                 : (Connection.Encoding ?? Encoding.UTF8)
                    .GetString(Convert.FromBase64String(base64String));
        }

        private void Authenticate(string username, string password)
        {
            // TODO: Here is where we'd do real authentication, if we needed the security.  
            // A starting point might be to maintain a username/password dictionary in the
            // server settings and compare to that; later implementations might perform an 
            // LDAP bind against a configured domain controller, etc.

            // NOTE: If we were serious about security, we should probably implement a
            // a method to read a SecureString directly from the network stream without
            // ever storing in an intermediary string, but this would require temporarily
            // wrapping the network stream in a CryptoStream with FromBase64Transform, 
            // reading each Char directly and inserting into the SecureString, which might
            // be better served by a ReadBase64TextStream method in ConnectionExtensions

            // For our, non-secure purposes, we just make sure we have a username and a
            // password, and that the password is not null -- we are only testing that the
            // upstream SmtpClient implementation correctly passes something (i.e. that
            // a developer implementing SmtpClient to send has remembered to configure
            // SMTP authentication options

            // In any case, if authentication is successful, set the Session.Username to 
            // the authenticated username, so the other commands know we're authenticated

            if (!string.IsNullOrWhiteSpace(username) &&
                password != null)
            {
                Session.Username = username;
                Connection.Logger?.Information("Authenticated as '{0}'", Session.Username);
            }
        }
    }
}
