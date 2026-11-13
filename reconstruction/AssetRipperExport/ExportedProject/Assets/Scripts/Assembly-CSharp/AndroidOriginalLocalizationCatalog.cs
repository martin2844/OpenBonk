using System;
using System.Collections.Generic;

// Bundle names and counts for the installed Megabonk 2023.2.22f1 build.
public static class AndroidOriginalLocalizationCatalog
{
    public sealed class Language
    {
        public string Code; public string Bundle; public int Entries;
        public string StoredFile => Code == "es" ? "strings.bundle" : "strings-" + Code + ".bundle";
    }
    public static readonly Language[] All = {
        new Language { Code="zh-Hans", Bundle="localization-string-tables-chinese(simplified)(zh-hans)_assets_all.bundle", Entries=1127 },
        new Language { Code="zh-Hant", Bundle="localization-string-tables-chinese(traditional)(zh-hant)_assets_all.bundle", Entries=1127 },
        new Language { Code="cs", Bundle="localization-string-tables-czech(cs)_assets_all.bundle", Entries=1127 },
        new Language { Code="en", Bundle="localization-string-tables-english(en)_assets_all.bundle", Entries=1129 },
        new Language { Code="fr", Bundle="localization-string-tables-french(fr)_assets_all.bundle", Entries=1127 },
        new Language { Code="de", Bundle="localization-string-tables-german(de)_assets_all.bundle", Entries=1127 },
        new Language { Code="it", Bundle="localization-string-tables-italian(it)_assets_all.bundle", Entries=1127 },
        new Language { Code="ja", Bundle="localization-string-tables-japanese(ja)_assets_all.bundle", Entries=1127 },
        new Language { Code="ko", Bundle="localization-string-tables-korean(ko)_assets_all.bundle", Entries=1127 },
        new Language { Code="pl", Bundle="localization-string-tables-polish(pl)_assets_all.bundle", Entries=1126 },
        new Language { Code="pt", Bundle="localization-string-tables-portuguese(pt)_assets_all.bundle", Entries=1127 },
        new Language { Code="ru", Bundle="localization-string-tables-russian(ru)_assets_all.bundle", Entries=1127 },
        new Language { Code="es", Bundle="localization-string-tables-spanish(es)_assets_all.bundle", Entries=1127 },
        new Language { Code="es-419", Bundle="localization-string-tables-spanish_latam_assets_all.bundle", Entries=1127 },
        new Language { Code="th", Bundle="localization-string-tables-thai(th)_assets_all.bundle", Entries=1127 },
        new Language { Code="tr", Bundle="localization-string-tables-turkish(tr)_assets_all.bundle", Entries=1126 },
        new Language { Code="uk", Bundle="localization-string-tables-ukrainian(uk)_assets_all.bundle", Entries=1125 },
    };
    public static Language Find(string code) { foreach (var language in All) if (string.Equals(language.Code, code, StringComparison.Ordinal)) return language; return null; }
}
