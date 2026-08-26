# ============================================
# NEXUS DEEPCODE LAUNCHER
# ============================================

# Load central Nexus secrets
. "C:\Personal\UserSecrets\Load-Secrets.ps1"

# DeepSeek configuration
$env:ANTHROPIC_BASE_URL = "https://api.deepseek.com/anthropic"

$env:ANTHROPIC_AUTH_TOKEN = $env:DEEPSEEK_API_KEY
$env:ANTHROPIC_API_KEY     = $env:DEEPSEEK_API_KEY

$env:CLAUDE_CODE_ENABLE_GATEWAY_MODEL_DISCOVERY = "1"
$env:CLAUDE_CODE_SKIP_FAST_MODE_ORG_CHECK = "1"

$env:ANTHROPIC_DEFAULT_SONNET_MODEL = "deepseek-v4-flash"
$env:ANTHROPIC_DEFAULT_OPUS_MODEL   = "deepseek-v4-flash"
$env:ANTHROPIC_DEFAULT_HAIKU_MODEL  = "deepseek-v4-flash"

if ([string]::IsNullOrWhiteSpace($env:DEEPSEEK_API_KEY)) {
    Write-Host "ERROR: DEEPSEEK_API_KEY not found."
    exit 1
}

Write-Host ""
Write-Host "========================================"
Write-Host "       NEXUS DEEPCODE"
Write-Host "========================================"
Write-Host ""
Write-Host "Provider : DeepSeek"
Write-Host "Model    : deepseek-v4-flash"
Write-Host "Folder   : $PWD"
Write-Host ""

claude