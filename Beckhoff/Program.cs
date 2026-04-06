using AmlParser.Modular.Controller;
using AmlParser.Modular.Service;

IParsingController controller = new ParsingController(new GvlXmlService());

return controller.Run(args);
