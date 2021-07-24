Import-Module PowershellForXti -Force

$script:tempLogConfig = [PSCustomObject]@{
    RepoOwner = "JasonBenfield"
    RepoName = "XTI_TempLog"
    AppName = "XTI_TempLog"
    AppType = "Package"
}

function TempLog-NewVersion {
    param(
        [Parameter(Position=0)]
        [ValidateSet("major", "minor", "patch")]
        $VersionType = "minor"
    )
    $script:tempLogConfig | New-XtiVersion @PsBoundParameters
}

function TempLog-NewIssue {
    param(
        [Parameter(Mandatory, Position=0)]
        [string] $IssueTitle,
        [switch] $Start
    )
    $script:tempLogConfig | New-XtiIssue @PsBoundParameters
}

function TempLog-StartIssue {
    param(
        [Parameter(Position=0)]
        [long]$IssueNumber = 0
    )
    $script:tempLogConfig | Xti-StartIssue @PsBoundParameters
}

function TempLog-CompleteIssue {
    param(
    )
    $script:tempLogConfig | Xti-CompleteIssue @PsBoundParameters
}

function TempLog-Publish {
    param(
        [ValidateSet("Development", "Production", "Staging", "Test")]
        $EnvName = "Development"
    )
    $script:tempLogConfig | Xti-Publish @PsBoundParameters
}
