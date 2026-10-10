# Unity CLI を使う。エディタ解決とレポート出力を CLI に任せる。
# CLI 未導入の場合は https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 を参照。
#
# 重要: 他の Unity プロジェクトが同時に開かれている場合があるため、
# プロジェクトパスは必ず明示する。auto-detect に頼ると別プロジェクトを操作しうる。
UNITY_PROJECT = ./Unity

# status / command の --project-path は「絶対パスへのサブストリング照合」であり、
# "./Unity" もスラッシュ区切りの絶対パスも一致しない。照合用の文字列を別に持つ。
UNITY_MATCH = UVMBinding

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

# プレイヤー上での PlayMode テスト。実機・IL2CPP・ストリッピング下の合否を機械判定する。
# unity run は batchmode 固定でプレイヤーテストが動かないため unity open --args を使う。
# -buildPlayerPath を渡さないと Temp/UnityTempFile-<GUID> に出るため必ず指定する。
# プレイヤーは結果を返すために Editor と通信する。ファイアウォールの規則は
# 実行ファイルのパス単位なので、固定しないと毎回許可を求められる。
# 実行前に Editor を閉じること。
PLAYER_TEST_DIR = ${CURDIR}/Unity/Build/PlayerTests

test-player:
	unity open ${UNITY_PROJECT} --non-interactive --args "-runTests -testPlatform StandaloneWindows64 -buildPlayerPath ${PLAYER_TEST_DIR} -testResults ${CURDIR}/TestResults.Player.xml -logFile ${CURDIR}/Unity/Build/player-tests.log"

# 稼働中の Editor への接続確認。state が ready にならない場合は
# Editor の起動と com.unity.pipeline の導入を確認する。
editor-status:
	unity status --project-path ${UNITY_MATCH} --no-banner

# Editor を開く。ブロックするためバックグラウンドで実行すること。
# コンパイルエラーがあると Safe Mode で起動し CLI から接続できない。
# 先に make test を通してから開く。
editor-open:
	unity open ${UNITY_PROJECT} --non-interactive

# 全 Editor インスタンスの一覧。パス絞り込みが無いため対象行を目視で確認する。
editor-list:
	unity pipeline list --no-banner

# IL2CPP + ストリッピング下でショーケースを実行し、基準値と突き合わせる。
# 差分が出たら [SerializeReference] の型が落ちたか、バインドが壊れている。
# プレイヤーは自己診断を書き出した後に自分で終了する。
# 同一プロジェクトを二重に開けないため、実行前に Editor を閉じること。
VERIFY_DIR = ./Unity/Build/Showcase
VERIFY_BASELINE = ./Unity/Verify/selfcheck-baseline.txt

verify-strip:
	unity run ${UNITY_PROJECT} --non-interactive -- -executeMethod ShowcaseBuilder.BuildPlayer
	${VERIFY_DIR}/Showcase.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -selfcheck-quit
	diff ${VERIFY_BASELINE} ${VERIFY_DIR}/selfcheck.txt
	@echo "OK: IL2CPP + Stripping High の結果が基準値と一致"

# アイコンは SVG が原本。Unity が取り込むのは書き出した PNG。
# 手で PNG を直さない。SVG を直して再生成する
ICON_DIR = ./Unity/Assets/UVMBinding.Sample/Art/Icons

icons:
	for f in ${ICON_DIR}/*.svg; do 		magick -background none "$$f" -resize 128x128 "$${f%.svg}.png"; 	done
	@echo "OK: $$(ls ${ICON_DIR}/*.png | wc -l) 個のアイコンを書き出した"

# 情景と敵は Art/Images/ が原本（生成物をそのまま置く。触らない）。
# ここから切り出しと背景抜きをして Art/Scenes/ と Art/Enemies/ を作る。
# 手で加工しない。原本を差し替えて再生成する
ART_DIR = ./Unity/Assets/UVMBinding.Sample/Art

art:
	@mkdir -p ${ART_DIR}/Scenes ${ART_DIR}/Enemies
	# 3:2 の上下 1/5 を落として中央帯を 1280x512 にする
	magick ${ART_DIR}/Images/01_forest_three_way_fork.png               -crop 1536x614+0+205 +repage -resize 1280x512! ${ART_DIR}/Scenes/scene_crossroads.png
	magick ${ART_DIR}/Images/02_medieval_apothecary_interior.png        -crop 1536x614+0+205 +repage -resize 1280x512! ${ART_DIR}/Scenes/scene_shop.png
	magick ${ART_DIR}/Images/03_dim_medieval_tavern_interior.png        -crop 1536x614+0+205 +repage -resize 1280x512! ${ART_DIR}/Scenes/scene_tavern.png
	magick ${ART_DIR}/Images/04_narrow_rocky_mountain_pass.png          -crop 1536x614+0+205 +repage -resize 1280x512! ${ART_DIR}/Scenes/scene_pass.png
	magick ${ART_DIR}/Images/05_dark_cave_entrance_in_rock_wall.png     -crop 1536x614+0+205 +repage -resize 1280x512! ${ART_DIR}/Scenes/scene_cave.png
	magick ${ART_DIR}/Images/06_wide_gentle_river_and_bank_path.png     -crop 1536x614+0+205 +repage -resize 1280x512! ${ART_DIR}/Scenes/scene_river.png
	magick ${ART_DIR}/Images/07_walled_city_gate_from_road.png          -crop 1536x614+0+205 +repage -resize 1280x512! ${ART_DIR}/Scenes/scene_gate.png
	magick ${ART_DIR}/Images/08_single_weathered_gravestone_wasteland.png -crop 1536x614+0+205 +repage -resize 1280x512! ${ART_DIR}/Scenes/scene_grave.png
	# 隅から流し込みで生成りを抜く。全面置換にすると内側の同色まで消える
	magick ${ART_DIR}/Images/09_gaunt_stray_dog_enemy.png                -alpha set -bordercolor "#E8DCC0" -border 1 -fuzz 8% -fill none -floodfill +0+0 "#E8DCC0" -shave 1x1 -resize 512x512 ${ART_DIR}/Enemies/enemy_dog.png
	magick ${ART_DIR}/Images/10_hooded_bandit_with_short_curved_blade.png -alpha set -bordercolor "#E8DCC0" -border 1 -fuzz 8% -fill none -floodfill +0+0 "#E8DCC0" -shave 1x1 -resize 512x512 ${ART_DIR}/Enemies/enemy_bandit.png
	magick ${ART_DIR}/Images/11_cracked_stone_monster_enemy.png          -alpha set -bordercolor "#E8DCC0" -border 1 -fuzz 8% -fill none -floodfill +0+0 "#E8DCC0" -shave 1x1 -resize 512x512 ${ART_DIR}/Enemies/enemy_eater.png
	@echo "OK: 情景 8 枚 / 敵 3 体を書き出した"
