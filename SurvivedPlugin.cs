using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace Survived;

[BepInPlugin("com.debugextract.survived", "debug_extract Survived", "1.0.0")]
[BepInProcess("EscapeFromTarkov.exe")]
public class SurvivedPlugin : BaseUnityPlugin
{
	private ConfigEntry<bool> _enabled;

	private ConfigEntry<KeyboardShortcut> _activationKey;

	private ConfigEntry<string> _commandText;

	private ConfigEntry<bool> _logToConsole;

	private void Awake()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		_enabled = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "Enabled", true, "Enables the hotkey command trigger.");
		_commandText = ((BaseUnityPlugin)this).Config.Bind<string>("General", "Console Command", "debug_extract Survived", "The command sent to the in-game console when the hotkey is pressed.");
		_activationKey = ((BaseUnityPlugin)this).Config.Bind<KeyboardShortcut>("General", "Activation Key", new KeyboardShortcut((KeyCode)290, Array.Empty<KeyCode>()), "Press this key to run the configured command.");
		_logToConsole = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "Log To Console", true, "Write extra logs while the trigger runs.");
		((BaseUnityPlugin)this).Logger.LogInfo((object)"debug_extract Survived loaded.");
		((BaseUnityPlugin)this).Logger.LogInfo((object)("Configured command: " + _commandText.Value));
		ManualLogSource logger = ((BaseUnityPlugin)this).Logger;
		KeyboardShortcut value = _activationKey.Value;
		object arg = ((KeyboardShortcut)(ref value)).MainKey;
		value = _activationKey.Value;
		logger.LogInfo((object)$"Activation key: {arg} + {((KeyboardShortcut)(ref value)).Modifiers}");
	}

	private void Update()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		if (_enabled.Value)
		{
			KeyboardShortcut value = _activationKey.Value;
			if (((KeyboardShortcut)(ref value)).IsDown())
			{
				TriggerConsoleCommand();
			}
		}
	}

	private void TriggerConsoleCommand()
	{
		string value = _commandText.Value;
		if (string.IsNullOrWhiteSpace(value))
		{
			((BaseUnityPlugin)this).Logger.LogWarning((object)"Console command is empty.");
			return;
		}
		if (_logToConsole.Value)
		{
			((BaseUnityPlugin)this).Logger.LogInfo((object)("Executing console command: " + value));
		}
		try
		{
			if (TryInvokeConsoleCommand(value))
			{
				((BaseUnityPlugin)this).Logger.LogInfo((object)("Successfully sent command: " + value));
			}
			else
			{
				((BaseUnityPlugin)this).Logger.LogWarning((object)("No compatible console command API was found for: " + value));
			}
		}
		catch (Exception arg)
		{
			((BaseUnityPlugin)this).Logger.LogError((object)$"Failed to execute console command: {arg}");
		}
	}

	private bool TryInvokeConsoleCommand(string command)
	{
		if (TryInvokeKnownConsoleMethods(command))
		{
			return true;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly == null)
			{
				continue;
			}
			try
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (!LooksLikeConsoleType(type))
					{
						continue;
					}
					MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
					foreach (MethodInfo methodInfo in methods)
					{
						if (!LooksLikeCommandMethod(methodInfo))
						{
							continue;
						}
						try
						{
							if (methodInfo.GetParameters().Length == 1 && (methodInfo.GetParameters()[0].ParameterType == typeof(string) || methodInfo.GetParameters()[0].ParameterType == typeof(object)))
							{
								object methodTarget = GetMethodTarget(type, methodInfo);
								methodInfo.Invoke(methodTarget, new object[1] { command });
								return true;
							}
							if (methodInfo.GetParameters().Length == 0)
							{
								object methodTarget2 = GetMethodTarget(type, methodInfo);
								methodInfo.Invoke(methodTarget2, null);
								return true;
							}
						}
						catch
						{
						}
					}
				}
			}
			catch
			{
			}
		}
		return false;
	}

	private bool TryInvokeKnownConsoleMethods(string command)
	{
		string[] source = new string[13]
		{
			"ConsoleScreen", "ConsoleCommand", "CommandProcessor", "CommandHandler", "Terminal", "DebugConsole", "ConsoleManager", "ChatConsole", "InputConsole", "DebugInput",
			"CommandLine", "ConsoleView", "TerminalController"
		};
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly == null)
			{
				continue;
			}
			try
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					string typeName = type.FullName ?? type.Name;
					if (!source.Any((string value) => typeName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0))
					{
						continue;
					}
					MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
					foreach (MethodInfo methodInfo in methods)
					{
						string name = methodInfo.Name;
						if ((name.IndexOf("Execute", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Process", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Parse", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Command", StringComparison.OrdinalIgnoreCase) >= 0) && methodInfo.GetParameters().Length == 1 && (methodInfo.GetParameters()[0].ParameterType == typeof(string) || methodInfo.GetParameters()[0].ParameterType == typeof(object)))
						{
							try
							{
								object methodTarget = GetMethodTarget(type, methodInfo);
								methodInfo.Invoke(methodTarget, new object[1] { command });
								return true;
							}
							catch
							{
							}
						}
					}
				}
			}
			catch
			{
			}
		}
		return false;
	}

	private bool LooksLikeConsoleType(Type type)
	{
		if (type == null || type.Name == null)
		{
			return false;
		}
		string text = type.FullName ?? type.Name;
		return text.IndexOf("Console", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Command", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Terminal", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Input", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Chat", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private bool LooksLikeCommandMethod(MethodInfo method)
	{
		if (method == null || method.IsSpecialName)
		{
			return false;
		}
		string name = method.Name;
		if (method.GetParameters().Length > 1)
		{
			return false;
		}
		return name.IndexOf("Execute", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Process", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Parse", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Input", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Command", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Console", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Invoke", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private object GetMethodTarget(Type type, MethodInfo method)
	{
		try
		{
			PropertyInfo property = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null && property.PropertyType == type)
			{
				return property.GetValue(null);
			}
			FieldInfo field = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == type)
			{
				return field.GetValue(null);
			}
			PropertyInfo property2 = type.GetProperty("Current", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (property2 != null && property2.PropertyType == type)
			{
				return property2.GetValue(null);
			}
			if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
			{
				return Activator.CreateInstance(type);
			}
		}
		catch
		{
		}
		return null;
	}
}
