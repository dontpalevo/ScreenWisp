# Localization and DPI validation

English and Russian UI strings live in Localization/Strings.resx and Strings.ru-RU.resx. ResourceManager supplies neutral English fallback. Localizer provides WPF bindings, live WinForms tray notifications, formatting and localized validation exceptions. There is one UI implementation.

Settings.Language accepts auto/en-US/ru-RU. Missing/unknown values normalize to auto without resetting the profile. Automatic uses Windows UI culture captured at process startup: Russian → ru-RU, everything else → en-US. CurrentCulture, enum names, numeric serialization and shortcut notation are unchanged. Language saves immediately; unsaved form edits remain. Failed preference persistence rolls back the selector.

App-owned titles, tray menu/status, popup, colors, hints, accessibility names and error messages follow the selected language. Windows-owned title-bar menus, native dialog buttons and security prompts follow Windows. Inno keeps its existing English/Russian language mechanism with a translated launch action.

365 Windows WPF/Win32/resource/profile assertions plus four lifecycle assertions pass, including legacy profiles, resource/placeholder parity, automatic fallback, live switching, failed save rollback and Alt/Z/cleanup/click-through regressions. Synthetic input is not a physical keyboard test.

Both languages are measured/rendered at 100/125/150/200% in WPF, with popup text-fit checks. These are render/layout checks, not actual changes to monitor DPI. Real English/Russian settings and popup screenshots were captured on the current desktop from production UI controls on a neutral backdrop. Only the screenshot fixture exposes the tool window to capture; product nonactivation is unchanged.

Manual acceptance: real Windows display scales 100/125/150/200%, both languages, settings scroll to Save, validation errors/dropdowns, tray menu/tooltip, popup at taskbar edges and on mixed-DPI monitors. Check real English/Russian/other-language Windows profiles, actual sign-in startup, clean VM, physical double Alt/Z/release on two monitors, Alt+Tab, NVIDIA fallback, Esc and emergency exit.
