using System;

public class MenuController 
{

    private readonly MenuModel menuModel;
    private readonly IMenuNavigation menuNavigation;

    public MenuController(MenuModel model, IMenuNavigation navigation)
    {
        if(model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }
        if(navigation == null)
        {
            throw new ArgumentNullException(nameof(navigation));
        }
        menuModel = model;
        menuNavigation = navigation;
    }
    public void OpenExplanation()
    {
        menuModel.SetExplanationOpen(true);
    }
    public void CloseExplanation()
    {
        menuModel.SetExplanationOpen(false);
    }
    public void StartGame()
    {
        menuNavigation.StartGame();
    }
    public void ExitGame()
    {
        menuNavigation.ExitGame();
    }
}
