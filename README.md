# SRD Obsidian Importer

This is the script I use to import d20pfsrd and 5esrd into Obsidian.

### Important Note
This was uploaded to GitHub as an example of how to construct your own script to do something like this. This is not a script that can just be cloned and executed with the expectation of it working.
I take no responsibility for the script not working as intended or any damage caused by this script or any modifications of it.

### How to use
Despite that, it should work now.
The script is only tested on windows!

A quick tutorial on how to use this script:
- clone it
- run `Program.cs`
- a text file (the config) should open in notepad or something similar
    - edit this config file
    - save and close the config file
- wait...
- script magic
    - the script will check if the HTML files are already there and if not it will scrape the selected site
    - after that, it will convert the files to markdown
    - then it will do another conversion step to convert all the links
    - then it will copy over any files that are supposed to be overridden
- the result will be in the run directory in a folder with `_md` at the end

### Any problems with the script?
Check the issues here on GitHub to see if the issue is already reported. 
If not feel free to create a new issue describing the problem you encountered.
Alternatively, you can message me on discord: Lemons#5466
