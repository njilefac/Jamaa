!include "MUI2.nsh"

!ifndef APP_NAME
    !define APP_NAME "Jamaa"
!endif
!define COMPANY_NAME "Nubia Systems"
!ifndef APP_VERSION
    !define APP_VERSION "1.0.0"
!endif

Name "${APP_NAME}"
OutFile "${OUTPUT_FILE}"
InstallDir "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "Software\${COMPANY_NAME}\${APP_NAME}" "InstallDir"
RequestExecutionLevel admin
Icon "${ICON_FILE}"
UninstallIcon "${ICON_FILE}"

!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_INSTFILESPAGE_NOAUTOCLOSE
!define MUI_FINISHPAGE_NOAUTOCLOSE
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

SetCompressor /SOLID zlib
Section "Install"
    SetOutPath "$INSTDIR"
    File /r "${INPUT_DIR}\*"

    WriteRegStr HKLM "Software\${COMPANY_NAME}\${APP_NAME}" "InstallDir" "$INSTDIR"
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortCut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\Jamaa.exe"
    CreateShortCut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\Jamaa.exe"
SectionEnd

Section "Uninstall"
    Delete "$DESKTOP\${APP_NAME}.lnk"
    Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
    RMDir "$SMPROGRAMS\${APP_NAME}"
    Delete "$INSTDIR\Uninstall.exe"
    RMDir /r "$INSTDIR"
    DeleteRegKey HKLM "Software\${COMPANY_NAME}\${APP_NAME}"
SectionEnd
