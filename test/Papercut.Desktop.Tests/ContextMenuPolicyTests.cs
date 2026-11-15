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
using NUnit.Framework;

namespace Papercut.Desktop.Tests;

[TestFixture]
public class ContextMenuPolicyTests
{
    private static ContextMenuPlan Plan(
        bool editable = false, bool link = false, bool selection = false, bool image = false, bool mainFrame = true, bool inspect = false) =>
        ContextMenuPolicy.Plan(new ContextMenuTarget(editable, link, selection, image, mainFrame), inspect);

    [Test]
    public void Blank_app_space_gets_no_menu()
    {
        Plan().IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Browser_items_never_survive()
    {
        var everything = new[]
        {
            Plan(), Plan(editable: true), Plan(link: true, selection: true, image: true, mainFrame: false)
        };

        foreach (var plan in everything)
        {
            plan.Keep.Should().NotContain(["back", "forward", "reload", "saveAs", "print", "share", "createQrCode", "inspectElement"]);
        }
    }

    [Test]
    public void A_link_offers_open_and_copy()
    {
        var plan = Plan(link: true, mainFrame: false);

        plan.OpenLink.Should().BeTrue();
        plan.Keep.Should().Equal("copyLinkLocation");
    }

    [Test]
    public void Selected_text_can_be_copied()
    {
        Plan(selection: true).Keep.Should().Equal("copy");
    }

    [Test]
    public void Select_all_is_for_the_email_not_the_app()
    {
        Plan(mainFrame: false).Keep.Should().Equal("selectAll");
        Plan(mainFrame: true).IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Text_fields_keep_editing()
    {
        var plan = Plan(editable: true);

        plan.OpenLink.Should().BeFalse();
        plan.Keep.Should().Equal("undo", "redo", "cut", "copy", "paste", "pasteAndMatchStyle", "selectAll");
    }

    [Test]
    public void Inspect_only_when_allowed()
    {
        Plan(inspect: true).Keep.Should().Equal("inspectElement");
    }
}
