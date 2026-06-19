#!/usr/bin/env bash
set -euo pipefail

script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
solution_root="$script_root/.."
project_path="$solution_root/Jamaa.Desktop/Jamaa.Desktop.csproj"
publish_dir="$solution_root/publish/linux"
package_root="$script_root/debroot"
package_name="jamaa"
version="${1:-1.0.0}"
# Ensure architecture-dependent names match expectation
architecture="amd64"
package_dir="$package_root/${package_name}_${version}_${architecture}"
deb_file="$script_root/${package_name}_${version}_${architecture}.deb"
icon_path="$solution_root/Jamaa.Desktop/Assets/Icons/jamaa.png"

rm -rf "$publish_dir" "$package_root" "$deb_file"

dotnet publish "$project_path" -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=false -p:PublishReadyToRun=false -o "$publish_dir" -p:Version="$version"

install -d \
  "$package_dir/DEBIAN" \
  "$package_dir/opt/jamaa" \
  "$package_dir/usr/bin" \
  "$package_dir/usr/share/applications" \
  "$package_dir/usr/share/icons/hicolor/256x256/apps"

cp -R "$publish_dir"/. "$package_dir/opt/jamaa/"
cat > "$package_dir/usr/bin/jamaa" <<'EOF'
#!/usr/bin/env bash
exec /opt/jamaa/Jamaa "$@"
EOF
chmod 755 "$package_dir/usr/bin/jamaa"

cat > "$package_dir/usr/share/applications/jamaa.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Jamaa
Comment=Association management application
Exec=/usr/bin/jamaa
Icon=jamaa
Terminal=false
Categories=Office;Finance;
EOF

cp "$icon_path" "$package_dir/usr/share/icons/hicolor/256x256/apps/jamaa.png"

cat > "$package_dir/DEBIAN/control" <<EOF
Package: $package_name
Version: $version
Section: utils
Priority: optional
Architecture: $architecture
Maintainer: Nubia Systems
Description: Jamaa desktop application
EOF

dpkg-deb --build --root-owner-group "$package_dir" "$deb_file"
