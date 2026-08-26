param(
    [string]$Type = "Claude Code",
    [string]$Message = "Needs attention"
)

$topic = "nexus-dev-2026"

curl.exe `
    -s `
    --max-time 5 `
    -H "Title: Nexus - $Type" `
    -H "Priority: 5" `
    -d "$Message" `
    "https://ntfy.sh/$topic" | Out-Null

exit 0