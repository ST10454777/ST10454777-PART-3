using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CybersecurityChatbotGUI
{
public partial class MainWindow : Window
{
private ChatbotEngine engine;

public MainWindow()
{
InitializeComponent();
engine = new ChatbotEngine();

// VOICE GREETING - FIXED PATH for Part 3
// File must be: Assets/greeting.wav + Copy to Output = Copy if newer
try
{
string wavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "greeting.wav");
// Fallback: also try root folder if Assets not found
if (!File.Exists(wavPath))
wavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "greeting.wav");

if (File.Exists(wavPath))
{
var player = new SoundPlayer(wavPath);
player.Play();
}
}
catch
{
// Audio missing - still run, don't crash
}

AddBotMessage("Hello! I'm Cyber-Guard, your Cybersecurity Awareness Chatbot.\n\nWhat's your name? And what's your favourite cybersecurity topic?\n\nTry:\n- Add task - Enable 2FA\n- Remind me to update my password tomorrow\n- Start quiz\n- Show activity log");
UpdateMemoryLabel();
}

// ---------- SEND BUTTON + ENTER KEY ----------
private void SendButton_Click(object sender, RoutedEventArgs e) => ProcessInput();

private void UserInputBox_KeyDown(object sender, KeyEventArgs e)
{
if (e.Key == Key.Enter) ProcessInput();
}

private void ProcessInput()
{
string input = UserInputBox.Text.Trim();
if (string.IsNullOrEmpty(input)) return;

AddUserMessage(input);
UserInputBox.Clear();
SendToBot(input);
}

// Shared method - used by typing AND buttons
private void SendToBot(string input)
{
try
{
string response = engine.GetResponse(input);
AddBotMessage(response);
UpdateMemoryLabel();
}
catch (Exception)
{
AddBotMessage("Oops, something went wrong internally, but I'm still running. Could you rephrase?");
}
}

private void UpdateMemoryLabel()
{
MemoryLabel.Text = $"Memory: Name={engine.UserName ?? "Unknown"} | Favourite={engine.FavTopic ?? "None"} | LastTopic={engine.LastTopic ?? "None"} | Tasks={engine.Tasks.Count} | Log={engine.ActivityLog.Count}";
}

// ---------- PART 3 BUTTONS ----------
private void TasksButton_Click(object sender, RoutedEventArgs e)
{
AddUserMessage("Show my tasks");
SendToBot("show my tasks");
}

private void AddTaskButton_Click(object sender, RoutedEventArgs e)
{
// Shows user the format, then they type it
AddBotMessage("To add a task, type e.g.:\nAdd task - Review privacy settings\n\nOr with reminder:\nRemind me to update my password tomorrow\n\nWhat task do you want to add?");
UserInputBox.Focus();
}

private void QuizButton_Click(object sender, RoutedEventArgs e)
{
AddUserMessage("Start quiz");
SendToBot("start quiz");
}

private void LogButton_Click(object sender, RoutedEventArgs e)
{
AddUserMessage("Show activity log");
SendToBot("show activity log");
}

private void HelpButton_Click(object sender, RoutedEventArgs e)
{
AddBotMessage("HELP - What I can do:\n\n" +
"TASKS:\n- Add task - Enable 2FA\n- Remind me to update my password tomorrow\n- Show my tasks\n- Complete task 1\n- Delete task 1\n\n" +
"QUIZ:\n- Start quiz (12 questions, A/B/C/D + True/False)\n\n" +
"CHAT:\n- Ask about password, phishing, scam, privacy\n- Say 'Tell me more' for more tips\n\n" +
"LOG:\n- Show activity log\n- What have you done for me?");
}

// ---------- CHAT BUBBLES ----------
private void AddUserMessage(string text)
{
var tb = new TextBlock
{
Text = $"You: {text}",
Margin = new Thickness(5),
FontWeight = FontWeights.Bold,
TextWrapping = TextWrapping.Wrap,
Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
};
ChatPanel.Children.Add(tb);
ChatScroll.ScrollToEnd();
}

private void AddBotMessage(string text)
{
var border = new Border
{
Background = new SolidColorBrush(Color.FromRgb(224, 242, 254)),
CornerRadius = new CornerRadius(8),
Margin = new Thickness(5),
Padding = new Thickness(10)
};
border.Child = new TextBlock
{
Text = $"Bot: {text}",
TextWrapping = TextWrapping.Wrap,
Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
};
ChatPanel.Children.Add(border);
ChatScroll.ScrollToEnd();
}
}
}
