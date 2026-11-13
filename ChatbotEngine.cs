using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CybersecurityChatbotGUI
{
// DELEGATES - Required for Part 2
public delegate string SentimentDelegate(string input, string baseResponse);
public delegate string ResponseDelegate(string keyword);

// TASK CLASS - Part 3 Task 1
public class CyberTask
{
public int Id { get; set; }
public string Title { get; set; }
public string Description { get; set; }
public DateTime? ReminderDate { get; set; }
public bool IsCompleted { get; set; }
public DateTime CreatedAt { get; set; } = DateTime.Now;
}

// QUIZ QUESTION CLASS - Part 3 Task 2
public class QuizQuestion
{
public string Question { get; set; }
public string[] Options { get; set; } // For True/False, use ["True","False"]
public string CorrectAnswer { get; set; } // e.g. "A" or "True"
public string Explanation { get; set; }
public bool IsTrueFalse { get; set; }
}

public class ChatbotEngine
{
// GENERIC COLLECTIONS - Required for Part 2
private Dictionary<string, List<string>> keywordResponses;
private List<string> phishingTips;
private Random random = new Random();

// MEMORY - Part 2
public string UserName { get; private set; }
public string FavTopic { get; private set; }
public string LastTopic { get; private set; }

// PART 3 - ACTIVITY LOG
public List<string> ActivityLog { get; private set; } = new List<string>();

// PART 3 - TASKS
public List<CyberTask> Tasks { get; private set; } = new List<CyberTask>();
private CyberTask pendingTask = null; // Waiting for reminder answer
private bool waitingForReminder = false;
private string tasksFile = "tasks.txt";

// PART 3 - QUIZ
private List<QuizQuestion> quizQuestions;
private int currentQuizIndex = -1;
private int quizScore = 0;
private bool inQuiz = false;

public ChatbotEngine()
{
InitCollections();
InitQuiz();
LoadTasks(); // Load from file = Database simulation
LogActivity("Chatbot started");
}

// ---------------- ACTIVITY LOG ----------------
private void LogActivity(string action)
{
string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm} - {action}";
ActivityLog.Add(entry);
// Keep only last 50, we show last 10
if (ActivityLog.Count > 50) ActivityLog.RemoveAt(0);
}

public string ShowActivityLog(int count = 10)
{
if (ActivityLog.Count == 0) return "No activity yet. Try adding a task or starting the quiz!";
var recent = ActivityLog.Skip(Math.Max(0, ActivityLog.Count - count)).ToList();
string result = "Here's a summary of recent actions:\n";
for (int i = 0; i < recent.Count; i++)
result += $"{i + 1}. {recent[i]}\n";
LogActivity("User viewed activity log");
return result;
}

// ---------------- TASKS - DATABASE METHODS ----------------
// These methods act like MySQL: Save / Load / Delete / Update
// If your lecturer wants real MySQL, swap the file code for MySqlConnection here
private void SaveTasks()
{
try
{
var lines = Tasks.Select(t => $"{t.Id}|{t.Title}|{t.Description}|{t.ReminderDate}|{t.IsCompleted}|{t.CreatedAt}");
File.WriteAllLines(tasksFile, lines);
}
catch { }
}

private void LoadTasks()
{
try
{
if (!File.Exists(tasksFile)) return;
foreach (var line in File.ReadAllLines(tasksFile))
{
var p = line.Split('|');
if (p.Length < 6) continue;
Tasks.Add(new CyberTask
{
Id = int.Parse(p[0]),
Title = p[1],
Description = p[2],
ReminderDate = string.IsNullOrEmpty(p[3]) ? null : (DateTime?)DateTime.Parse(p[3]),
IsCompleted = bool.Parse(p[4]),
CreatedAt = DateTime.Parse(p[5])
});
}
}
catch { }
}

public string AddTask(string title, string description, DateTime? reminder)
{
var task = new CyberTask
{
Id = Tasks.Count == 0 ? 1 : Tasks.Max(t => t.Id) + 1,
Title = title,
Description = description,
ReminderDate = reminder,
IsCompleted = false
};
Tasks.Add(task);
SaveTasks();
LogActivity($"Task added: '{title}'" + (reminder != null ? $" (Reminder set for {reminder.Value:yyyy-MM-dd})" : " (No reminder)"));
return task.Id.ToString();
}

public string ShowTasks()
{
if (Tasks.Count == 0) return "You have no tasks yet. Say 'Add task - Enable 2FA' to add one.";
string result = "Your tasks:\n";
foreach (var t in Tasks)
{
string status = t.IsCompleted ? "[Done]" : "[Pending]";
string rem = t.ReminderDate != null ? $" Reminder: {t.ReminderDate.Value:yyyy-MM-dd}" : " No reminder";
result += $"{t.Id}. {status} {t.Title} - {t.Description} |{rem}\n";
}
return result;
}

public string CompleteTask(int id)
{
var t = Tasks.FirstOrDefault(x => x.Id == id);
if (t == null) return $"Task {id} not found.";
t.IsCompleted = true;
SaveTasks();
LogActivity($"Task completed: '{t.Title}'");
return $"Done! Task '{t.Title}' marked as completed.";
}

public string DeleteTask(int id)
{
var t = Tasks.FirstOrDefault(x => x.Id == id);
if (t == null) return $"Task {id} not found.";
Tasks.Remove(t);
SaveTasks();
LogActivity($"Task deleted: '{t.Title}'");
return $"Task '{t.Title}' deleted.";
}

// ---------------- QUIZ ----------------
private void InitQuiz()
{
quizQuestions = new List<QuizQuestion>
{
new QuizQuestion{ Question="What is phishing?", Options=new[]{"A) A type of fishing","B) Fake email to steal info","C) Antivirus software","D) A password manager"}, CorrectAnswer="B", Explanation="Phishing is a fake email/message pretending to be trusted to steal your info.", IsTrueFalse=false },
new QuizQuestion{ Question="A strong password should be at least 12 characters.", Options=new[]{"True","False"}, CorrectAnswer="True", Explanation="Longer passwords are harder to crack. Use 12+ characters.", IsTrueFalse=true },
new QuizQuestion{ Question="What should you do with a suspicious link?", Options=new[]{"A) Click it fast","B) Hover to check URL, don't click","C) Forward to friends","D) Reply with password"}, CorrectAnswer="B", Explanation="Always hover to see the real URL. Never click suspicious links.", IsTrueFalse=false },
new QuizQuestion{ Question="Legit companies ask for your password by email.", Options=new[]{"True","False"}, CorrectAnswer="False", Explanation="No legit company asks for passwords by email. That's phishing.", IsTrueFalse=true },
new QuizQuestion{ Question="What is 2FA?", Options=new[]{"A) Two passwords","B) Two-step verification","C) Two computers","D) Two emails"}, CorrectAnswer="B", Explanation="2FA = second code from phone/app, so stolen password alone is not enough.", IsTrueFalse=false },
new QuizQuestion{ Question="You should use the same password for all accounts.", Options=new[]{"True","False"}, CorrectAnswer="False", Explanation="If one site leaks, all accounts fall. Use unique passwords.", IsTrueFalse=true },
new QuizQuestion{ Question="What is social engineering?", Options=new[]{"A) Building bridges","B) Tricking people to give info","C) Social media app","D) Engineering degree"}, CorrectAnswer="B", Explanation="Social engineering tricks people, not computers, to give info.", IsTrueFalse=false },
new QuizQuestion{ Question="Public Wi-Fi is always safe for banking.", Options=new[]{"True","False"}, CorrectAnswer="False", Explanation="Public Wi-Fi can be spied on. Use VPN or mobile data for banking.", IsTrueFalse=true },
new QuizQuestion{ Question="Which is safest for a password?", Options=new[]{"A) 123456","B) Your birthday","C) BlueSky$Rain2026!","D) password"}, CorrectAnswer="C", Explanation="Mix upper, lower, numbers, symbols. Avoid personal info.", IsTrueFalse=false },
new QuizQuestion{ Question="You should update software regularly.", Options=new[]{"True","False"}, CorrectAnswer="True", Explanation="Updates fix security holes hackers exploit.", IsTrueFalse=true },
new QuizQuestion{ Question="What to do if you get a 'Your account will be closed!' email?", Options=new[]{"A) Click link immediately","B) Go to official site/app directly","C) Reply with password","D) Ignore and delete"}, CorrectAnswer="B", Explanation="Scammers create urgency. Always go to the official site yourself.", IsTrueFalse=false },
new QuizQuestion{ Question="A flashlight app needs access to your contacts.", Options=new[]{"True","False"}, CorrectAnswer="False", Explanation="Check permissions. A flashlight does not need contacts.", IsTrueFalse=true },
};
}

private string StartQuiz()
{
inQuiz = true;
currentQuizIndex = 0;
quizScore = 0;
LogActivity("Quiz started");
return "Cybersecurity Quiz started! Answer with A, B, C, D or True/False.\n\n" + GetCurrentQuestion();
}

private string GetCurrentQuestion()
{
var q = quizQuestions[currentQuizIndex];
string text = $"Q{currentQuizIndex + 1}/{quizQuestions.Count}: {q.Question}\n";
for (int i = 0; i < q.Options.Length; i++) text += q.Options[i] + "\n";
return text;
}

private string AnswerQuiz(string input)
{
var q = quizQuestions[currentQuizIndex];
string ans = input.Trim().ToUpper();
// Allow "B" or "B) ..." or "True"
if (ans.Length > 1) ans = ans.Substring(0, 1);
if (q.IsTrueFalse) ans = input.Trim(); // Keep True/False full
// Normalise True/False
if (q.IsTrueFalse)
{
ans = char.ToUpper(ans[0]) + ans.Substring(1).ToLower();
}

bool correct = ans.Equals(q.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
if (correct) quizScore++;

string feedback = correct ? "Correct! " : $"Wrong. Correct answer: {q.CorrectAnswer}. ";
feedback += q.Explanation + "\n\n";

currentQuizIndex++;
if (currentQuizIndex >= quizQuestions.Count)
{
inQuiz = false;
LogActivity($"Quiz completed - {quizScore}/{quizQuestions.Count} answered correctly");
string finalMsg = quizScore >= 9 ? "Great job! You're a cybersecurity pro!" : quizScore >= 6 ? "Good effort! Keep learning to stay safe online!" : "Keep learning to stay safe online!";
return feedback + $"Quiz finished! Score: {quizScore}/{quizQuestions.Count}\n{finalMsg}";
}
return feedback + GetCurrentQuestion();
}

// ---------------- PART 2 COLLECTIONS ----------------
private void InitCollections()
{
keywordResponses = new Dictionary<string, List<string>>
{
["password"] = new List<string> {
"Make sure to use strong, unique passwords for each account. Avoid using personal details in your passwords.",
"Tip: Use a passphrase like 'BlueSky$Rain2026!' and a password manager. Never reuse passwords.",
"Password Safety: 12+ characters, mix of upper/lower, numbers, symbols. Change if breached."
},
["scam"] = new List<string> {
"Scam Alert: If it sounds too good to be true, it is. Verify sender emails carefully.",
"Don't click links from unknown numbers. Scammers often create urgency - 'Your account will be closed!'"
},
["privacy"] = new List<string> {
"Privacy Tip: Review app permissions. Does a flashlight app need your contacts? No.",
"Check your social media privacy settings quarterly. Limit who sees your personal info."
},
["phishing"] = new List<string>()
};

phishingTips = new List<string>
{
"Be cautious of emails asking for personal information. Scammers often disguise themselves as trusted organisations.",
"Phishing Tip: Hover over links to see the real URL before clicking. Look for misspellings like amaz0n.com.",
"Phishing Tip: Legit companies never ask for your password via email. Report phishing to your IT team.",
"Tip: Enable 2FA - even if they steal your password, they can't get in."
};
}

// ---------------- MAIN RESPONSE ----------------
public string GetResponse(string userInput)
{
if (string.IsNullOrWhiteSpace(userInput)) return "I didn't catch that. Could you type something?";
string inputLower = userInput.ToLower().Trim();

// 1. QUIZ MODE - if in quiz, treat input as answer
if (inQuiz)
{
if (inputLower == "quit" || inputLower == "exit quiz") { inQuiz = false; return "Quiz stopped. Your score was not saved."; }
return AnswerQuiz(userInput);
}

// 2. WAITING FOR REMINDER ANSWER - Part 3 Task 1 flow
if (waitingForReminder && pendingTask != null)
{
waitingForReminder = false;
if (inputLower.Contains("yes") || inputLower.Contains("remind"))
{
DateTime? rem = ParseReminder(inputLower);
if (rem == null)
{
// Ask again with example, keep pending
waitingForReminder = true;
return "When should I remind you? Say e.g. 'in 3 days' or 'tomorrow' or 'in 5 days'.";
}
pendingTask.ReminderDate = rem;
// Now actually save it
Tasks.Add(pendingTask);
SaveTasks();
LogActivity($"Task added: '{pendingTask.Title}' (Reminder set for {rem.Value:yyyy-MM-dd})");
string t = pendingTask.Title;
pendingTask = null;
return $"Got it! I'll remind you about '{t}' on {rem.Value:yyyy-MM-dd}.";
}
else
{
Tasks.Add(pendingTask);
SaveTasks();
LogActivity($"Task added: '{pendingTask.Title}' (No reminder)");
string t = pendingTask.Title;
pendingTask = null;
return $"Okay, task '{t}' added with no reminder.";
}
}

SentimentDelegate sentimentHandler = HandleSentiment;

// 3. ACTIVITY LOG - Part 3 Task 4
if (inputLower.Contains("show activity log") || inputLower.Contains("what have you done") || inputLower.Contains("activity log") || inputLower.Contains("what did you do"))
return ShowActivityLog(10);

if (inputLower.Contains("show more") && ActivityLog.Count > 10)
return ShowActivityLog(20);

// 4. QUIZ START - Part 3 Task 2
if (inputLower.Contains("quiz") || inputLower.Contains("game") || inputLower.Contains("start quiz") || inputLower.Contains("play"))
return StartQuiz();

// 5. TASKS - Part 3 Task 1 + NLP
if (inputLower.Contains("show my tasks") || inputLower.Contains("view tasks") || inputLower.Contains("my tasks") || inputLower.Contains("list tasks"))
return ShowTasks();

if (inputLower.Contains("complete task") || inputLower.Contains("mark task"))
{
int id = ExtractNumber(inputLower);
if (id == -1) return "Which task? Say e.g. 'Complete task 1'.";
return CompleteTask(id);
}
if (inputLower.Contains("delete task") || inputLower.Contains("remove task"))
{
int id = ExtractNumber(inputLower);
if (id == -1) return "Which task? Say e.g. 'Delete task 1'.";
return DeleteTask(id);
}

// NLP: Add task in many wordings
if (inputLower.Contains("add task") || inputLower.Contains("create task") || inputLower.Contains("remind me") || inputLower.Contains("add a task") || inputLower.Contains("i need to"))
{
string title = ExtractTaskTitle(userInput);
DateTime? rem = ParseReminder(inputLower);
LogActivity($"NLP interpreted task command: '{userInput}'");

// If reminder already in same sentence, save directly
if (rem != null)
{
AddTask(title, title, rem);
return $"Got it! Task '{title}' added. I'll remind you on {rem.Value:yyyy-MM-dd}.";
}
// If user said "remind me" but no date, ask for date
if (inputLower.Contains("remind me"))
{
pendingTask = new CyberTask { Title = title, Description = title, CreatedAt = DateTime.Now };
waitingForReminder = true;
return $"Task '{title}' noted. When should I remind you? Say e.g. 'in 3 days' or 'tomorrow'.";
}
// Normal add task - ask if reminder wanted
pendingTask = new CyberTask { Title = title, Description = title, CreatedAt = DateTime.Now };
waitingForReminder = true;
return $"Task '{title}' noted. Would you like a reminder? Say 'Yes, in 3 days' or 'No'.";
}

// 6. MEMORY - FIXED: Only real name phrases
if (inputLower.Contains("my name is") || inputLower.Contains("call me"))
{
UserName = ExtractName(userInput);
LogActivity($"User set name to {UserName}");
return $"Great! I'll remember that your name is {UserName}.";
}
if (inputLower.Contains("interested in") || inputLower.Contains("favourite") || inputLower.Contains("favorite") || inputLower.Contains("i like"))
{
FavTopic = DetectFavTopic(inputLower);
LogActivity($"User favourite topic: {FavTopic}");
return $"Great! I'll remember that you're interested in {FavTopic}.";
}

// 7. CONVERSATION FLOW
if (inputLower.Contains("another tip") || inputLower.Contains("tell me more") || inputLower.Contains("explain more"))
{
if (LastTopic != null)
return sentimentHandler(inputLower, $"Sure, here's more on {LastTopic}: {GetRandomResponseForTopic(LastTopic)}");
return "Of course! What topic? Try: password, scam, privacy, or phishing.";
}

// 8. KEYWORD RECOGNITION
foreach (var key in keywordResponses.Keys)
{
if (inputLower.Contains(key))
{
LastTopic = key;
string baseResponse = (key == "phishing") ? phishingTips[random.Next(phishingTips.Count)] : keywordResponses[key][random.Next(keywordResponses[key].Count)];
LogActivity($"NLP responded to keyword '{key}'");
string finalResponse = sentimentHandler(inputLower, baseResponse);
if (UserName != null) finalResponse = $"{UserName}, {finalResponse}";
return finalResponse;
}
}

// 9. UNKNOWN
string tip = phishingTips[random.Next(phishingTips.Count)];
return sentimentHandler(inputLower, $"I'm not sure I understand. Can you try rephrasing? In the meantime, here's a tip: {tip}\nTry: 'Add task', 'Start quiz', 'Show activity log', 'Show my tasks'");
}

// ---------------- HELPERS ----------------
private string HandleSentiment(string input, string baseResponse)
{
if (input.Contains("worried") || input.Contains("anxious") || input.Contains("scared"))
return $"It's completely understandable to feel that way. Scammers can be very convincing. {baseResponse}";
if (input.Contains("frustrated") || input.Contains("confused") || input.Contains("overwhelmed"))
return $"I hear you, it can be overwhelming. Let's take it step by step. {baseResponse}";
if (input.Contains("curious") || input.Contains("interested"))
return $"Love your curiosity! Knowledge is your best defense. {baseResponse}";
return baseResponse;
}

private string GetRandomResponseForTopic(string topic)
{
ResponseDelegate responder = (k) => {
if (k == "phishing") return phishingTips[random.Next(phishingTips.Count)];
return keywordResponses.ContainsKey(k) ? keywordResponses[k][random.Next(keywordResponses[k].Count)] : "Keep your software updated!";
};
return responder(topic);
}

private string ExtractName(string input)
{
string lower = input.ToLower();
if (lower.Contains("my name is")) return input.Substring(lower.IndexOf("my name is") + 10).Trim().Trim('.', '!', '?');
if (lower.Contains("call me")) return input.Substring(lower.IndexOf("call me") + 7).Trim().Trim('.', '!', '?');
return input.Split(' ').Last().Trim('.', '!', '?');
}

private string DetectFavTopic(string input)
{
if (input.Contains("password")) return "password";
if (input.Contains("privacy")) return "privacy";
if (input.Contains("scam")) return "scam";
if (input.Contains("phishing")) return "phishing";
return "cybersecurity";
}

private string ExtractTaskTitle(string input)
{
string lower = input.ToLower();
string[] prefixes = { "add task -", "add task:", "add task", "create task", "remind me to", "remind me", "add a task to", "i need to" };
string title = input;
foreach (var p in prefixes)
{
if (lower.Contains(p))
{
int idx = lower.IndexOf(p) + p.Length;
title = input.Substring(idx).Trim();
break;
}
}
// Remove reminder part: "in 3 days", "tomorrow"
int remIdx = title.ToLower().IndexOf(" remind");
if (remIdx > 0) title = title.Substring(0, remIdx).Trim();
// Remove trailing " in 3 days"
if (title.ToLower().Contains(" in ")) title = title.Substring(0, title.ToLower().IndexOf(" in ")).Trim();
if (string.IsNullOrWhiteSpace(title)) title = "General cybersecurity task";
// Capitalise first letter
return char.ToUpper(title[0]) + title.Substring(1);
}

private DateTime? ParseReminder(string lower)
{
if (lower.Contains("tomorrow")) return DateTime.Now.Date.AddDays(1);
if (lower.Contains("today")) return DateTime.Now.Date;
// "in 3 days", "in 5 days"
int idx = lower.IndexOf("in ");
if (idx >= 0)
{
string after = lower.Substring(idx + 3).Trim();
string[] parts = after.Split(' ');
if (parts.Length > 0 && int.TryParse(parts[0], out int days))
return DateTime.Now.Date.AddDays(days);
}
return null;
}

private int ExtractNumber(string lower)
{
foreach (var word in lower.Split(' '))
if (int.TryParse(word, out int n)) return n;
return -1;
}
}
}
