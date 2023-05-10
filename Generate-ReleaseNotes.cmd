rem https://github.com/StefH/GitHubReleaseNotes

SET version=3.2.0

GitHubReleaseNotes --output CHANGELOG.md --skip-empty-releases --exclude-labels question invalid doc duplicate --version %version% --token %GH_TOKEN%