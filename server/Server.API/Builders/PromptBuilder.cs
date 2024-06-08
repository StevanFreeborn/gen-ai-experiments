namespace Server.API.Builders;

interface IPromptBuilder
{
  string CreateAutoCompletePrompt(string userInput);
  string CreateWritingAssistantPrompt(string userInput, string existingContent);
  string CreateControlToCitationMappingPrompt(string controlText, List<Citation> citations);
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

  public string CreateWritingAssistantPrompt(string userInput, string existingContent) => @$"""
    You are an AI writing assistant. Your task is to help the user complete a writing assignment based on their prompt and any existing content they provide.

    Here is the user's writing prompt:
    <prompt>
    {userInput}
    </prompt>

    If the user has provided any existing content for their writing task, it will be included below:
    <existing_content>
    {existingContent}
    </existing_content>

    Please carefully read the writing prompt to understand what kind of writing task the user needs help with. 

    <scratchpad>
    If existing content was provided:
    - Consider how to use the existing content as a starting point for completing the writing task.
    - Think about how to expand upon, improve, or finish the existing content based on the prompt.

    If no existing content was provided: 
    - Consider how to approach the writing task from scratch based solely on the prompt.
    - Think about what key points, sections, or ideas to include to best fulfill the writing task.
    </scratchpad>

    Now, please complete the writing task for the user. Focus on being helpful, engaging, and providing the highest quality content possible to assist them. 

    Format your writing response in properly structured HTML that would be suitable for inserting into the body of a webpage.

    For example:
    ```html
    <h1>Title of the Writing Task</h1>
    <p>Here is the content that you generate to fulfill the writing task...</p>
    <h2>Subheading</h2>
    <p>Additional content...</p>
    ```

    Remember, properly structure your HTML ensuring that all tags are opened and closed. DO NOT include any partial or incomplete HTML in your response.

    Respond only with your properly structured HTML writing completion. Do not include any other text, notes, or explanations in your response.
  """;

  public string CreateControlToCitationMappingPrompt(string controlText, List<Citation> citations) => @$"""
    You will be given the text of a control and a list of citations. Your task is to determine which citations, if any, map to or are relevant to the given control text.

    The control text will be provided inside <control_text> tags like this:
    <control_text>
    {controlText}
    </control_text>

    The list of possible citations will be provided inside <citations> tags, with each individual citation inside its own <citation> tag like this:
    <citations>
    {string.Join("\n", citations.Select(c => c.ToString()))}
    </citations>

    Please read carefully the control text and each citation text. You DO NOT need to restate them in your output.

    Finally a JSON object that contains a `mappings` property whose value is an array of the ids as numbers of the citations you determined map to the control text. The id of each citation is provided in the ""id"" attribute of the <citation> tag. If you determine that no citations map to the control, simply set the `mappings` property to `null`.

    For example:
    ```json
    {{ ""mappings"": [1,2,3] }}
    ```

    Remember DO NOT provide any more output besides the JSON object.
  """;
}