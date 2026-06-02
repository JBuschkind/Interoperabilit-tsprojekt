using XmlParser.Modular.Controller;
using XmlParser.Modular.Service;

IParsingController controller = new ParsingController(
    new GvlXmlService(),
    new CSharpToGvlXmlService(),
    new GvlCsToXmlService());

return controller.Run(args);
