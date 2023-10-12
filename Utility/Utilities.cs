using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTraso.Utility;

public static class Utilities
{
    /// <summary>
    /// creates a filepath to retrieve files from the local program directory.
    /// </summary>
    /// <param name="relativeFileLocation">The filepath to append.</param>
    /// <returns>The full filepath.</returns>
    public static string GetLocalFilePath(string relativeFileLocation) => Path.Combine(AppContext.BaseDirectory, "Files", relativeFileLocation);

    /// <summary>
    /// A method to disable all buttons in a <see cref="ComponentBuilder"/>.
    /// </summary>
    /// <param name="buttonBuilder">The <see cref="ComponentBuilder"/> to get the buttons from.</param>
    /// <returns>A new <see cref="ComponentBuilder"/> with buttons disabled in i.t</returns>
    public static ComponentBuilder DisableAllButtons(ComponentBuilder buttonBuilder)
    {
        var newButtonBuilder = new ComponentBuilder();

        var rows = buttonBuilder.ActionRows;

        for (int i = 0; i < rows.Count; i++)
        {
            foreach (var component in rows[i].Components)
            {
                switch (component)
                {
                    case ButtonComponent button:
                        newButtonBuilder.WithButton(button.ToBuilder()
                            .WithDisabled(true), i);
                        break;
                    case SelectMenuComponent menu:
                        newButtonBuilder.WithSelectMenu(menu.ToBuilder()
                            .WithDisabled(true), i);
                        break;
                }
            }
        }
        return newButtonBuilder;
    }
    /// <summary>
    /// Creates a combination of 2 timestamps so it can show the full date and time including seconds.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTimeOffset"/> to base the timestamps off.</param>
    /// <returns>A <see langword="string"/> with 2 timestamps to make the full datetime.</returns>
    public static string FullDateTimeStamp(DateTimeOffset dateTime)
    {
        return TimestampTag.FormatFromDateTimeOffset(dateTime, TimestampTagStyles.LongDate) +
                " " + TimestampTag.FormatFromDateTimeOffset(dateTime, TimestampTagStyles.LongTime);
    }
}
