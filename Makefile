# Unity CLI を使う。エディタ解決とレポート出力を CLI に任せる。
# CLI 未導入の場合は https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 を参照。
#
# 重要: 他の Unity プロジェクトが同時に開かれている場合があるため、
# プロジェクトパスは必ず明示する。auto-detect に頼ると別プロジェクトを操作しうる。
UNITY_PROJECT = ./Unity

doc-build:
	docfx build docfx/docfx.json -t statictoc

doc-metadata:
	docfx metadata docfx/docfx.json

format:
	cd Unity && ./format.sh

rebuild-project:
	rm -f ./Unity/*.csproj
	rm -f ./Unity/*.sln
	rm -f ./Unity/*.slnx
	rm -rf ./Unity/Library/ScriptAssemblies
	unity run ${UNITY_PROJECT} --non-interactive

doc-release: rebuild-project doc-metadata
	rm -rf docfx/_site
	rm -rf Documents
	docfx build docfx/docfx.json
	cp -r docfx/_site Documents

# PlayMode が主軸。実機 / IL2CPP / ストリッピング下の挙動を検証できるため。
test:
	unity test ${UNITY_PROJECT} --mode PlayMode --report-format nunit,junit \
		--output ./TestResults.PlayMode.xml --junit-output ./TestResults.PlayMode.junit.xml \
		--non-interactive

test-edit:
	unity test ${UNITY_PROJECT} --mode EditMode --report-format nunit,junit \
		--output ./TestResults.EditMode.xml --junit-output ./TestResults.EditMode.junit.xml \
		--non-interactive

test-all: test test-edit

# 稼働中の Editor への接続確認。ready にならない場合は Editor の起動と
# com.unity.pipeline の導入（unity pipeline install）を確認する。
editor-status:
	unity status --project-path ${UNITY_PROJECT} --no-banner

# 全 Editor インスタンスの一覧。パス絞り込みが無いため、対象行を目視で確認する。
editor-list:
	unity pipeline list --no-banner
