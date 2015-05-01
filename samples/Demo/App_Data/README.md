This file is needed to make sure that the `App_Data` directory is not empty
otherwise a DVCS like Mercurial or Git will skip adding it as part of a
repo. The directory needs to exist so that the demo project does not throw
surprising errors when an ELMAH `ErrorLog` implementation expects `App_Data`
to exist for logging errors into it.
