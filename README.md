**AvitoChecker** is a scraper for Avito and Youla marketplaces. It currently works only witn Windows 10.

To run it, simply modify the sample appsettings.json file that is created in the build output directory.

Among other options, the configuration file contains Category parameters for both currently supported services. Valid values can be obtained by manually creating the needed search query in both services and taking the category part out of the resulting URL. 

E.g.

For https://www.avito.ru/rossiya/tovary_dlya_kompyutera/komplektuyuschie/materinskie_platy-ASgBAgICAkTGB~pm7gnOZw?cd=1&q=b550 

Use:
```
"AvitoOptions": {
      _____
      "Category": "tovary_dlya_kompyutera/komplektuyuschie/materinskie_platy"
    }
```
The random letters can be ommited in the ase of Avito

The tool stores any seen listings in the file specified by the `JSONFileStorageOptions` section.

For any new listing that the tool finds, it shows a Windows notification. Clicking the notification opens the related marketplace page.
