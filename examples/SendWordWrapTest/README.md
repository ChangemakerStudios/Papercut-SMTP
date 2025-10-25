# SendWordWrapTest

Test email sender for CSS word-wrap functionality (Issue #154).

## Description

This example demonstrates the fix for issue #154 by sending an HTML email with various test cases for CSS word-wrapping behavior. The email contains long unbroken text strings in different HTML elements to verify that word-wrapping works correctly without horizontal scrolling.

## What It Tests

The test email includes 6 comprehensive test cases:

1. **Long text in paragraph without word-wrap CSS** - Tests default wrapping behavior
2. **Paragraph with explicit word-wrap CSS** - Tests `word-wrap: break-word`
3. **Code element with word-wrap CSS** - Tests `overflow-wrap: break-word` and `white-space: pre-wrap`
4. **Pre element with word-break CSS** - Tests `word-break: break-all`
5. **Div wrapper** - Tests wrapping in container elements
6. **Long URLs** - Tests wrapping of long hyperlinks

## Usage

### Prerequisites

- Papercut SMTP must be running on localhost:25 (default configuration)
- Or modify `appsettings.json` to point to your SMTP server

### Run the Example

```bash
# From the project root
dotnet run --project examples/SendWordWrapTest

# Or from the example directory
cd examples/SendWordWrapTest
dotnet run
```

### Configuration

SMTP settings are configured in the shared `../appsettings.json` file. Edit it to customize:

```json
{
  "SmtpSend": {
    "Host": "127.0.0.1",
    "Port": 25,
    "Security": "None",
    "Username": null,
    "Password": null
  }
}
```

## Expected Results

When viewing the email in Papercut:

- ✅ All long text strings should wrap within the viewport
- ✅ No horizontal scrolling should be required
- ✅ Text should break at appropriate boundaries
- ✅ CSS word-wrap properties should be honored on all elements

## Technical Details

The test email is generated from [../resources/test-word-wrap.html](../resources/test-word-wrap.html), which contains various HTML elements with long unbroken strings to test the CSS word-wrap fix implemented in the `HtmlToHtmlFormatWrapper` template.

The HTML file is copied to the build output's `resources` directory during build (configured via `Directory.Build.props`).

## Related

- **Issue**: #154 - Does not support CSS force word wrap
- **Fix**: Added global CSS rules to HTML wrapper template
- **Test File**: `examples/resources/test-word-wrap.html`
