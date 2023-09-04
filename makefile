GAME_DIR=${HOME}/GOG Games/Stardew Valley/game
MOD_DIR_CP=${GAME_DIR}/Mods/HatMouseLacey
MOD_DIR_SMAPI=${GAME_DIR}/Mods/HatMouseLacey_Core

install: smapi cp

smapi:
	cd SMAPI && dotnet build

cp:
	mkdir -p "${MOD_DIR_CP}/assets" "${MOD_DIR_CP}/i18n"
	install -m 644 CP/content.json CP/manifest.json "${MOD_DIR_CP}/"
	install -m 644 CP/assets/* "${MOD_DIR_CP}/assets"
	install -m 644 CP/i18n/* "${MOD_DIR_CP}/i18n"

clean:
	rm -rf SMAPI/bin SMAPI/obj

uninstall:
	rm -rf "${MOD_DIR_CP}" "${MOD_DIR_SMAPI}"
