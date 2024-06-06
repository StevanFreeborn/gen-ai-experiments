namespace Server.API.Builders;

interface IPromptBuilder
{
  string CreateAutoCompletePrompt(string userInput);
}

class PromptBuilder : IPromptBuilder
{
  public string CreateAutoCompletePrompt(string userInput) => @$"""
    Your task is to provide an auto-completion of a text prompt, continuing the text in a natural and coherent way.

    First, carefully analyze the following writing prompt, paying close attention to the writing style, tone, content, and intent:

    <prompt>
    {userInput}
    </prompt>

    Your job is to continue writing from where this prompt left off, matching the style and content of the original prompt as closely as possible. Aim to write at least 50 additional words, but feel free to write more if needed to bring the text to a natural stopping point. Do not abruptly cut off the completion just to hit a word count.

    When you are done, output your full completion as a valid JSON object with a property of `completion`. The completion should NOT include the {userInput} text. For example:

    ```json
    {{ ""completion"": ""Additional text that you generate to naturally continue and extend the prompt..."" }}
    ```

    Remember, the key is to make the completion as seamless and natural of a continuation of the original prompt as possible, so that a reader would not be able to tell where the original ends and the AI-generated completion begins. Match the original prompt's style, tone, content and intent as much as possible in your completion.

    Remember, the response needs to be a valid JSON object with the completion as the value of a completion property. DO NOT output anything other than the JSON object.
  """;
}