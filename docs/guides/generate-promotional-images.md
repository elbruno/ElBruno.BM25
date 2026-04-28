# Generating Promotional Images for ElBruno.BM25

This guide explains how to generate professional promotional images for the ElBruno.BM25 NuGet package using the **t2i CLI** tool.

## Prerequisites

1. **Install t2i CLI** (global dotnet tool):
   ```powershell
   dotnet tool install -g ElBruno.Text2Image.Cli
   ```

2. **Obtain Microsoft Foundry Credentials**:
   - Endpoint URL: Your Microsoft Foundry API endpoint
   - API Key: Your FLUX.2 authentication key

## Quick Start

### Option 1: Set Credentials Interactively

```powershell
t2i secrets set foundry-flux2
```

This will prompt you to enter your Foundry endpoint and API key, storing them securely.

### Option 2: Set Credentials via Environment Variables

```powershell
# PowerShell
$env:T2I_FOUNDRY_FLUX2_ENDPOINT = "https://your-foundry-endpoint.com"
$env:T2I_FOUNDRY_FLUX2_API_KEY = "your-api-key-here"
```

```bash
# Bash/Linux
export T2I_FOUNDRY_FLUX2_ENDPOINT="https://your-foundry-endpoint.com"
export T2I_FOUNDRY_FLUX2_API_KEY="your-api-key-here"
```

### Option 3: Generate Images One at a Time

```powershell
cd C:\src\ElBruno.BM25

# Generate a single icon
t2i "BM25 search icon, minimalist design, tech logo, blue orange accents, transparent background" `
    --output "docs/images/nuget-icon-128x128.png" `
    --width 128 --height 128 `
    --provider foundry-flux2
```

## Generate All Images at Once

Use the provided PowerShell script to generate all 8 promotional images with a single command:

```powershell
cd C:\src\ElBruno.BM25
.\scripts\generate-promotional-images.ps1
```

The script will generate:

### Required Images

| Image | Dimensions | Purpose |
|-------|-----------|---------|
| `nuget-icon-128x128.png` | 128×128 | Primary NuGet package icon |
| `nuget-icon-64x64.png` | 64×64 | NuGet fallback icon |
| `nuget-icon-32x32.png` | 32×32 | NuGet smallest icon |

### Promotional Images

| Image | Dimensions | Purpose |
|-------|-----------|---------|
| `blog-hero.png` | 1200×630 | Blog post header |
| `linkedin-promo.png` | 1200×628 | LinkedIn promotional post |
| `twitter-announcement.png` | 1024×512 | Twitter/X announcement |
| `github-social-preview.png` | 1280×640 | GitHub repository social card |
| `docs-hero.png` | 1200×400 | Documentation page header |

## Design Specifications

### NuGet Icons
- **Requirements**: Transparent background, high contrast, tech/search themed, scalable
- **Theme**: BM25 algorithm concept, search and ranking
- **Colors**: Blue and orange accents
- **Style**: Minimalist, professional, modern

### Promotional Images
- **Style**: Modern tech aesthetic, clean minimalist design
- **Colors**: Blue and orange accents, professional color scheme
- **Branding**: Include ElBruno branding where appropriate
- **Text Overlays** (for announcement images):
  - "ElBruno.BM25 v0.5.0"
  - "Full-Text Search for .NET"

## Troubleshooting

### Error: "Missing secret 'apiKey' for provider 'foundry-flux2'"

Set up credentials using either method above:
```powershell
t2i secrets set foundry-flux2
```

### Error: "Generation failed: 401 Unauthorized"

Your API key is invalid or expired. Reconfigure:
```powershell
t2i secrets set foundry-flux2
```

### Error: "Generation failed: 429 Too Many Requests"

Rate limit exceeded. The script automatically adds 2-second delays between requests. To modify:
```powershell
.\scripts\generate-promotional-images.ps1 -DelayBetweenRequests 5
```

### Testing Your Setup

Verify credentials are working:
```powershell
t2i secrets test foundry-flux2
```

Check configuration:
```powershell
t2i doctor
```

## Manual Generation Commands

Generate images individually with specific prompts:

```powershell
cd C:\src\ElBruno.BM25

# NuGet Icons
t2i "BM25 search algorithm icon, minimalist design, tech logo, blue and orange accents, transparent background, professional, scalable" `
    --output "docs/images/nuget-icon-128x128.png" --width 128 --height 128

# Blog Hero
t2i "ElBruno.BM25 full-text search library hero image, C# .NET technology, modern design, blue orange tech aesthetic" `
    --output "docs/images/blog-hero.png" --width 1200 --height 630

# LinkedIn Promo
t2i "ElBruno.BM25 full-text search for .NET professional promotional image, modern tech design, blue orange accents" `
    --output "docs/images/linkedin-promo.png" --width 1200 --height 628

# Twitter Announcement
t2i "ElBruno.BM25 v0.5.0 announcement image, full-text search for .NET, modern tech aesthetic, blue orange" `
    --output "docs/images/twitter-announcement.png" --width 1024 --height 512

# GitHub Social Preview
t2i "ElBruno.BM25 GitHub repository social preview, full-text search library, C# .NET, modern professional design" `
    --output "docs/images/github-social-preview.png" --width 1280 --height 640

# Documentation Hero
t2i "ElBruno.BM25 documentation header, full-text search algorithm, BM25 ranking, modern tech design" `
    --output "docs/images/docs-hero.png" --width 1200 --height 400
```

## Verifying Generated Images

After generation, verify all files were created:

```powershell
# List all generated images
Get-ChildItem C:\src\ElBruno.BM25\docs\images\*.png | ForEach-Object {
    $size = $_.Length / 1KB
    Write-Host "$($_.Name): $([math]::Round($size, 2)) KB"
}
```

## CI/CD Integration

To integrate image generation into GitHub Actions, add this step to your workflow:

```yaml
- name: Generate promotional images
  env:
    T2I_FOUNDRY_FLUX2_ENDPOINT: ${{ secrets.T2I_FOUNDRY_FLUX2_ENDPOINT }}
    T2I_FOUNDRY_FLUX2_API_KEY: ${{ secrets.T2I_FOUNDRY_FLUX2_API_KEY }}
  run: |
    cd C:\src\ElBruno.BM25
    .\scripts\generate-promotional-images.ps1
```

## Additional Resources

- **t2i CLI Documentation**: [ElBruno.Text2Image GitHub](https://github.com/elbruno/ElBruno.Text2Image)
- **NuGet Package Repository**: [ElBruno.BM25 on NuGet.org](https://www.nuget.org/packages/ElBruno.BM25/)
- **Repository Setup Template**: [docs/Repository-Setup-Template.md](../Repository-Setup-Template.md)

## Support

For issues with:
- **t2i CLI**: See [t2i troubleshooting](https://github.com/elbruno/ElBruno.Text2Image/issues)
- **Image generation quality**: Adjust prompts in the script
- **API credentials**: Contact your Microsoft Foundry administrator
