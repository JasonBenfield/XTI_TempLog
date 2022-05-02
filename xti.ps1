Import-Module PowershellForXti -Force

function Xti-Publish {
    param(
        [ValidateSet("Development", "Production")]
        $EnvName = "Development",
        [ValidateSet("Default", "DB")]
        $HubAdministrationType = "Default"
    )
    BaseXti-Publish @PsBoundParameters
}
