using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// Управление общими сведениями о сборке осуществляется с помощью 
// атрибутов. Измените значения этих атрибутов, чтобы изменить сведения
// связанные со сборкой.
[assembly: AssemblyTitle("Steam Switcher")]
[assembly: AssemblyDescription("Fastest account switcher for Steam")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Steam Switcher")]
[assembly: AssemblyCopyright("Inflactwf © 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Установка значения False для параметра ComVisible делает типы в этой сборке невидимыми 
// для COM-компонентов.  Если требуется обратиться к типу в этой сборке через 
// COM, задайте для атрибута ComVisible значение True для этого типа.
[assembly: ComVisible(false)]

//Чтобы начать разработку локализуемых построений, задайте 
//<UICulture>CultureYouAreCodingWith</UICulture> в CSPROJ-файле
//внутри <PropertyGroup>.  Например, при использовании языка "Английский (США)"
//в файлах с исходным кодом установите значение <UICulture> равным "en-US".  Затем раскомментируйте
//атрибута NeutralResourceLanguage ниже.  Обновите "en-US" в
//строка внизу для обеспечения соответствия настройки UICulture в файле проекта.

//[сборка: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //где находятся словари ресурсов, связанные с темами
                                     //(используется, если ресурс не найден на странице 
                                     // или словари ресурсов приложения)
    ResourceDictionaryLocation.SourceAssembly //где находится универсальный словарь ресурсов
                                              //(используется, если ресурс не найден на странице 
                                              // приложения, или любые словари ресурсов, связанные с темами)
)]


// Сведения о версии сборки состоят из указанных ниже четырех значений:
//
//      Основной номер версии
//      Дополнительный номер версии 
//      Номер сборки
//      Редакция
//
// Можно задать все значения или принять номер сборки и номер редакции по умолчанию, 
// с помощью "*", как показано ниже:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
