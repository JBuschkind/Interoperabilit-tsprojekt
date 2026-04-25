using AmlParser.Modular.Controller;
using AmlParser.Modular.Service;

IParsingController controller = new ParsingController(
    new GvlXmlService(),
    new CSharpToGvlXmlService());

return controller.Run(args);
