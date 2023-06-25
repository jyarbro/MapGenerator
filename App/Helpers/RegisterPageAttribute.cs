namespace Nrrdio.MapGenerator.App.Helpers;

[AttributeUsage(AttributeTargets.Class)]
public class RegisterPageAttribute : Attribute {
    public Type ViewModel { get; }
    public Type Page { get; }

    public RegisterPageAttribute(Type viewmodel, Type page) {
        ViewModel = viewmodel;
        Page = page;
    }
}
