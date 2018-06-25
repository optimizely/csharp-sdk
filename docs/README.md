# Generate API Documentation
### Steps
* Download [DocFX](https://dotnet.github.io/docfx/) and put its path in **PATH environment variable**.
* Execute **docfx init -q -o documentation** in SDK root directory. It will create a folder named **documentation** in root directory.
* Replace **docfx.json** and **index.md** in the **documentation** directory with **docfx.json** and **index.md** provided in the **docs** folder.
* Replace **index.md** inside **api** folder of **documentation** directory with the one provided in the **docs/api** folder.
* Execute **docfx documentation/docfx.json --serve** in SDK root directory.
* This will generate HTML documentation in **documentation/csharp** directory.
* Browse **http://localhost:8080** in the browser.

### Notes
* Tool: [DocFX](https://dotnet.github.io/docfx/)
* The configuration file **docfx.json** is placed in the **docs** directory of SDK. Please see the configuration details [here](https://dotnet.github.io/docfx/tutorial/docfx.exe_user_manual.html).