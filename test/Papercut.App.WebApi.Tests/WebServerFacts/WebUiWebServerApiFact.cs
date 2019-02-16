// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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


namespace Papercut.Module.WebUI.Test.WebServerFacts
{
    using Base;

    using NUnit.Framework;


    [TestFixture]
    public class WebUiWebServerApiFact : ApiTestBase
    {
        [Test]
        public void ShouldBootstrapHttpServerAndServeHealthCheck()
        {
            var content = Get("/health").Content.ReadAsStringAsync().Result;

            Assert.AreEqual("Papercut WebUI server started successfully.", content);
        }
    }
}