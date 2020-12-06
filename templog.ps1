Import-Module PowershellForXti -Force

$script:tempLogConfig = [PSCustomObject]@{
    RepoOwner = "JasonBenfield"
    RepoName = "XTI_TempLog"
    AppName = "XTI_TempLog"
    AppType = "Package"
    ProjectDir = ""
}

function TempLog-New-XtiIssue {
    param(
        [Parameter(Mandatory, Position=0)]
        [string] $IssueTitle,
        $Labels = @(),
        [string] $Body = "",
        [switch] $Start
    )
    $script:tempLogConfig | New-XtiIssue @PsBoundParameters
}

function TempLog-Xti-StartIssue {
    param(
        [Parameter(Position=0)]
        [long]$IssueNumber = 0,
        $IssueBranchTitle = "",
        $AssignTo = ""
    )
    $script:tempLogConfig | Xti-StartIssue @PsBoundParameters
}

function TempLog-New-XtiVersion {
    param(
        [Parameter(Position=0)]
        [ValidateSet("major", "minor", "patch")]
        $VersionType = "minor",
        [ValidateSet("Development", "Production", "Staging", "Test")]
        $EnvName = "Production"
    )
    $script:tempLogConfig | New-XtiVersion @PsBoundParameters
}

function TempLog-New-XtiPullRequest {
    param(
        [Parameter(Position=0)]
        [string] $CommitMessage
    )
    $script:tempLogConfig | New-XtiPullRequest @PsBoundParameters
}

function TempLog-Xti-PostMerge {
    param(
    )
    $script:tempLogConfig | Xti-PostMerge @PsBoundParameters
}

function TempLog-Publish {
    param(
        [switch] $Prod
    )
    $script:tempLogConfig | Xti-PublishPackage @PsBoundParameters
}
