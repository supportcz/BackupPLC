This tool can be used to back up and restore SW from/to B&R PLC.
It is mainly usable for SG3 and SGC targets which do not have CF card.

How to backup SW from PLC:
-Unpack "BackupPLC.zip" into a directory. The directory will include:
  Modules...empty directory where the modules from PLC will be stored
  GeneratePIL.exe...this program generates "Backup.pil" a "Restore.pil" from "ModuleList.txt"
  GeneratePIL.pil...main starting batch, see next...
-Open "GeneratePIL.pil" in Runtime Utility Center (RUC)
-Modify the first line (i.e. connection to the PLC)
-Save the file (Ctrl+S)
-Start the batch (F5)
-After successful execution following files should occur in the same directory:
  ModuleList.txt...list of modules stored on PLC
  Backup.pil...batch for backup of modules
  Restore.pil...batch for restore of modules
  DownloadAR.pil...template for AR download
-Open "Backup.pil" in RUC and start the batch
-BR modules will be stored to "Modules" directory

How to restore SW back to PLC:
-Open "Restore.pil" in RUC and start the batch
-When the restore was succesfull, "ModuleListRestored.txt" file is created. This file contains list of modules on PLC.