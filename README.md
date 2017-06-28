# FileSync
A small program to keep your external storage in sync.

# Future features and improvements

- Turn computer off when done (or sleep, hibernate, execute powershell script, ...)

- Show progress based on file size, not count (would be cool to get feedback as copy of single file progresses)

- Split code in decent classes

- Black list files by name/extension/size

- Allow sync for deleted files if desired (delete files that are absent on one side !CAREFUL!)

- If deletion is implemented, give a list of files to be deleted before proceeding

- Compare even common files to propose to copy the most recent one on both sides

- Make recursivity optional by individual folder map

- Add an option to "install" the tool on external drives (will copy itself at the new drive's root)

- Log every operation on files (copy, deletion, overwrite)

- Keep an history of last few syncs in case we lose something (at least we would know who to blame...)

- Make the relative path transformation when adding a mapping more reliable (maybe allow the FileSync folder to not be at root)

- Add an icon

# Known bugs

- If both external and internal drive are selected as the same drive (same root), conditional sync (internal to external or vice versa) will not work. See TODO in code.

- Progress window display is all f-ed up (text boxes and progress label).

- Before creating folders if they don't exist, we should check if sync is enabled in that direction.
