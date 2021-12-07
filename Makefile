ifndef UNITY_APP
	UNITY_APP="C:/Program Files/Unity/Hub/Editor/2020.3.11f1/Editor/Unity.exe"
endif

doc-build:
	docfx build docfx/docfx.json -t statictoc

doc-metadata:
	docfx metadata docfx/docfx.json

format:
	cd Unity && ./format.sh

rebuild-project:
	echo ${PWD}
	rm -f ./Unity/*.csproj
	rm -f ./Unity/*.sln
	rm -rf ./Unity/Library/ScriptAssemblies
	${UNITY_APP}  -quit -batchmode -projectPath "${PWD}/Unity" -executeMethod "ILib.SolutionSync.Run"

doc-release: rebuild-project doc-metadata
	rm -rf docfx/_site
	rm -rf Documents
	docfx build docfx/docfx.json
	cp -r docfx/_site Documents
