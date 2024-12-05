using CleanArchitecture.CodeGenerator.Configuration;
using OpenAI.Chat;

namespace CleanArchitecture.CodeGenerator.Helpers;

public static class ChatGptHelper
{
    private static ChatClient _chatClient;

    /// <summary>
    /// Initializes the ChatGPT helper by retrieving the API key from the configuration file.
    /// </summary>
    public static void Initialize()
    {

        // Access the API key from ConfigurationHandler
        var configHandler = new ConfigurationHandler("appsettings.json");
        var configSettings = configHandler.GetConfiguration();
        string apiKey = configSettings.OpenAIApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("API key is missing in the configuration.");

        // Initialize the ChatClient with the retrieved API key
        _chatClient = new ChatClient("gpt-4", apiKey);
    }

    /// <summary>
    /// Sends a user message to ChatGPT and retrieves the assistant's response.
    /// Each call is independent and does not maintain a conversation history.
    /// </summary>
    /// <param name="userMessage">The user's message.</param>
    /// <returns>The assistant's response.</returns>
    public static async Task<string> SendMessageAsync(string userMessage)
    {
        if (_chatClient == null)
            throw new InvalidOperationException("ChatGptHelper is not initialized. Call Initialize() first.");

        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("User message cannot be null or empty.", nameof(userMessage));

        ChatMessage[] messages = new ChatMessage[]
    {
        new SystemChatMessage("You are an expert SEO content writer specializing in creating detailed, institution-specific information. Your primary role is to generate clean, plain HTML content that is fully formatted and optimized for SEO. The HTML output should be directly usable on any existing web page without further modification. Ensure the content is professional, concise, and tailored to the needs of educational institutions."),
        new UserChatMessage(userMessage)
    };

        // Get response from ChatGPT
        ChatCompletion response = await _chatClient.CompleteChatAsync(messages);
        return response.Content[0].Text.Trim();
    }
}