ifndef UNITY_APP
	UNITY_APP="C:/Program Files/Unity/Hub/Editor/6000.3.20f1/Editor/Unity.exe"
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
	rm -f ./Unity/*.slnx
	rm -rf ./Unity/Library/ScriptAssemblies
	${UNITY_APP}  -quit -batchmode -projectPath "${PWD}/Unity"

doc-release: rebuild-project doc-metadata
	rm -rf docfx/_site
	rm -rf Documents
	docfx build docfx/docfx.json
	cp -r docfx/_site Documents

test:
	${UNITY_APP} -runTests -batchmode -projectPath "${PWD}/Unity" -testPlatform EditMode -testResults "${PWD}/TestResults.xml" -logFile -

test-play:
	${UNITY_APP} -runTests -batchmode -projectPath "${PWD}/Unity" -testPlatform PlayMode -testResults "${PWD}/TestResults.PlayMode.xml" -logFile -
