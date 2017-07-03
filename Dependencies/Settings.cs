namespace Dependencies.Properties {
    
    
    // Cette classe vous permet de gérer des événements spécifiques dans la classe de paramètres :
    //  L'événement SettingChanging est déclenché avant la modification d'une valeur de paramètre.
    //  L'événement PropertyChanged est déclenché après la modification d'une valeur de paramètre.
    //  L'événement SettingsLoaded est déclenché après le chargement des valeurs de paramètre.
    //  L'événement SettingsSaving est déclenché avant l'enregistrement des valeurs de paramètre.
    internal sealed partial class Settings {
        
        public Settings() {
            // // Pour ajouter des gestionnaires d'événements afin d'enregistrer et de modifier les paramètres, supprimez les marques de commentaire des lignes ci-dessous :
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
        }
        
        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // Ajouter du code pour gérer l'événement SettingChangingEvent ici.
        }
        
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Ajouter du code pour gérer l'événement SettingsSaving ici.
        }
    }
}
