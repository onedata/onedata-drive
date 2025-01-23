using System.Configuration;

namespace OnedataDriveGUI
{
    [SettingsProvider(typeof(CustomSettingsProvider))]
    public class CustomSettings : ApplicationSettingsBase
    {
        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool RootFolderDeleteCheckBox
        {
            get => (bool)this[nameof(RootFolderDeleteCheckBox)];
            set => this[nameof(RootFolderDeleteCheckBox)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("")]
        public string RootFolderPath
        {
            get => (string)this[nameof(RootFolderPath)];
            set => this[nameof(RootFolderPath)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("")]
        public string OneproviderToken
        {
            get => (string)this[nameof(OneproviderToken)];
            set => this[nameof(OneproviderToken)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("")]
        public string Onezone
        {
            get => (string)this[nameof(Onezone)];
            set => this[nameof(Onezone)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool OneproviderTokenKeep
        {
            get => (bool)this[nameof(OneproviderTokenKeep)];
            set => this[nameof(OneproviderTokenKeep)] = value;
        }
    }

}
