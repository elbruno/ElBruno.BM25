#Requires -Version 7.0
<#
.SYNOPSIS
    Generate promotional images for ElBruno.BM25 NuGet package using t2i CLI

.DESCRIPTION
    This script generates all required promotional assets:
    - NuGet icons (128x128, 64x64, 32x32)
    - Blog hero image (1200x630)
    - LinkedIn promotional image (1200x628)
    - Twitter announcement image (1024x512)
    - GitHub social preview (1280x640)
    - Documentation header (1200x400)

.EXAMPLE
    # Set credentials first
    $env:T2I_FOUNDRY_FLUX2_ENDPOINT = "your-endpoint-url"
    $env:T2I_FOUNDRY_FLUX2_API_KEY = "your-api-key"
    
    # Then run the script
    .\scripts\generate-promotional-images.ps1

.NOTES
    Requires t2i CLI to be installed: dotnet tool install -g ElBruno.Text2Image.Cli
    Requires FLUX.2 API credentials configured
#>

param(
    [string]$OutputDir = "docs\images",
    [string]$Provider = "foundry-flux2",
    [int]$DelayBetweenRequests = 2  # seconds to respect rate limits
)

# Ensure we're in the project root
if (-not (Test-Path "docs\images")) {
    Write-Host "Error: Must run from project root directory" -ForegroundColor Red
    exit 1
}

# Check if t2i is installed
try {
    $t2iVersion = t2i version 2>&1
    Write-Host "✓ t2i installed: $t2iVersion" -ForegroundColor Green
}
catch {
    Write-Host "Error: t2i CLI not installed. Install with:" -ForegroundColor Red
    Write-Host "  dotnet tool install -g ElBruno.Text2Image.Cli" -ForegroundColor Yellow
    exit 1
}

# Test credentials
Write-Host ""
Write-Host "Testing t2i credentials..." -ForegroundColor Cyan
$testResult = t2i secrets test $Provider 2>&1
if ($testResult -match "error|failed|unauthorized" -or $LASTEXITCODE -ne 0) {
    Write-Host "Error: t2i credentials not configured properly" -ForegroundColor Red
    Write-Host "Configure with: t2i secrets set $Provider" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ Credentials verified" -ForegroundColor Green

# Image generation jobs
$images = @(
    @{
        name = "nuget-icon-128x128"
        prompt = "BM25 search algorithm icon, minimalist design, tech logo, blue and orange accents, transparent background, professional, scalable, 128x128"
        width = 128
        height = 128
        description = "NuGet package icon (128×128)"
    },
    @{
        name = "nuget-icon-64x64"
        prompt = "BM25 search algorithm icon, minimalist design, tech logo, blue and orange accents, transparent background, professional, 64x64"
        width = 64
        height = 64
        description = "NuGet package icon (64×64 fallback)"
    },
    @{
        name = "nuget-icon-32x32"
        prompt = "BM25 search algorithm icon, minimalist design, tech logo, blue and orange accents, transparent background, simple, 32x32"
        width = 32
        height = 32
        description = "NuGet package icon (32×32 smallest)"
    },
    @{
        name = "blog-hero"
        prompt = "ElBruno.BM25 full-text search library hero image, C# .NET technology, modern design, blue orange tech aesthetic, 1200x630"
        width = 1200
        height = 630
        description = "Blog post header image (1200×630)"
    },
    @{
        name = "linkedin-promo"
        prompt = "ElBruno.BM25 full-text search for .NET professional promotional image, modern tech design, blue orange accents, 1200x628"
        width = 1200
        height = 628
        description = "LinkedIn promotional image (1200×628)"
    },
    @{
        name = "twitter-announcement"
        prompt = "ElBruno.BM25 v0.5.0 announcement image, full-text search for .NET, modern tech aesthetic, blue orange, 1024x512"
        width = 1024
        height = 512
        description = "Twitter/X announcement image (1024×512)"
    },
    @{
        name = "github-social-preview"
        prompt = "ElBruno.BM25 GitHub repository social preview, full-text search library, C# .NET, modern professional design, 1280x640"
        width = 1280
        height = 640
        description = "GitHub social preview card (1280×640)"
    },
    @{
        name = "docs-hero"
        prompt = "ElBruno.BM25 documentation header, full-text search algorithm, BM25 ranking, modern tech design, 1200x400"
        width = 1200
        height = 400
        description = "Documentation header image (1200×400)"
    }
)

# Generate images
Write-Host ""
Write-Host "Generating promotional images..." -ForegroundColor Cyan
Write-Host ""

$successCount = 0
$failCount = 0

foreach ($image in $images) {
    $outputPath = Join-Path $OutputDir "$($image.name).png"
    Write-Host "[$($images.IndexOf($image) + 1)/$($images.Count)] Generating: $($image.description)" -ForegroundColor White
    
    try {
        t2i $image.prompt `
            --output $outputPath `
            --width $image.width `
            --height $image.height `
            --provider $Provider `
            | Out-Null
        
        # Verify file was created
        if (Test-Path $outputPath) {
            $fileSize = (Get-Item $outputPath).Length / 1KB
            Write-Host "  ✓ Created: $($image.name).png ($([math]::Round($fileSize, 2)) KB)" -ForegroundColor Green
            $successCount++
        }
        else {
            Write-Host "  ✗ Failed: File not created" -ForegroundColor Red
            $failCount++
        }
    }
    catch {
        Write-Host "  ✗ Error: $_" -ForegroundColor Red
        $failCount++
    }
    
    # Rate limiting (except on last iteration)
    if ($images.IndexOf($image) -lt ($images.Count - 1)) {
        Start-Sleep -Seconds $DelayBetweenRequests
    }
}

# Summary
Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  ✓ Generated: $successCount / $($images.Count) images" -ForegroundColor $(if ($successCount -eq $images.Count) { "Green" } else { "Yellow" })
if ($failCount -gt 0) {
    Write-Host "  ✗ Failed: $failCount" -ForegroundColor Red
}

# List generated files
Write-Host ""
Write-Host "Generated files in $OutputDir :" -ForegroundColor Cyan
Get-ChildItem -Path $OutputDir -Filter "*.png" -ErrorAction SilentlyContinue | ForEach-Object {
    $size = $_.Length / 1KB
    Write-Host "  • $($_.Name) ($([math]::Round($size, 2)) KB)" -ForegroundColor Green
}

Write-Host ""
if ($successCount -eq $images.Count) {
    Write-Host "✓ All images generated successfully!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "✗ Some images failed to generate. Check configuration and try again." -ForegroundColor Red
    exit 1
}
