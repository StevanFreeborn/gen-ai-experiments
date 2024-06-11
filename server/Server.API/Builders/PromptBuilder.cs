namespace Server.API.Builders;

interface IPromptBuilder
{
  string CreateAutoCompletePrompt(string userInput);
  string CreateWritingAssistantPrompt(string userInput, string existingContent);
  string CreateControlToCitationMappingPrompt(string controlText, List<Citation> citations);
  string CreateControlToCitationMappingPromptRevised(string controlText, List<Citation> citations);
  string CreateImportAnalysisPrompt(List<string> rows);
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

  public string CreateControlToCitationMappingPromptRevised(string controlText, List<Citation> citations) => @$"""
    You will be given the text of a control and a list of citations. Your task is to determine which citations, if any, map to or are relevant to the given control text.

    The control text will be provided inside <control_text> tags like this:
    <control_text>
      Check the accuracy of restricted data.
    </control_text>

    The list of possible citations will be provided inside <citations> tags, with each individual citation inside its own <citation> tag like this:
    <citations>
      <citation id=""1"">
        Confirms to the greatest extent practicable upon collection or creation of personally identifiable information (PII), the accuracy, relevance, timeliness, and completeness of that information;
      </citation>
      <citation id=""2"">
        Checks for, and corrects as necessary, any inaccurate or outdated PII used by its programs or systems [Assignment: organization-defined frequency]; and
      </citation>
    </citations>

    You will return a JSON object that contains a `mappings` property whose value is an array of the ids as numbers of the citations you determined map to the control text. The id of each citation is provided in the ""id"" attribute of the <citation> tag. If you determine that no citations map to the control, simply set the `mappings` property to `null`.

    For example:
    ```json
    {{ ""mappings"": [1,2] }}
    ```

    Here are the citations you will be working with:
    <citations>
      {string.Join("\n", citations.Select(c => c.ToString()))}
    </citations>

    Here is the control text you will be working with:
    <control_text>
      {controlText}
    </control_text>

    Please read carefully the control text and each citation text. Think about each citation step by step to determine if it maps to the control. You DO NOT need to restate them in your output.

    Remember DO NOT provide any more output besides the JSON object.
  """;

  public string CreateImportAnalysisPrompt(List<string> rows) => @$"""
    I will provide you with a CSV file containing data:

    <csv_file>
    {string.Join("\n", rows)}
    </csv_file>

    Your task is to summarize the structure and shape of this CSV data in a JSON object. The JSON object should include the following information for each column in the CSV:
    - The name of the column (property)
    - A brief description of what that column represents
    - The data type of the values in that column

    Here are the steps to complete this task:

    1. Parse the CSV data and identify the column names.
    2. For each column:
      a. Determine the data type by inspecting the values. If the column contains multiple data types, use the most prevalent one.
      b. Select ONLY from the following types:
          1. Date - Use for Date or Date + Time values such as 06-10-2024
          2. List - Use for categorical or label data such as Status, Category, Type, Language, Country, etc.
          4. Text - Use for alphanumeric characters such as HTML markup, json, multi-line text, emails, phone numbers, etc.
          5. Number - Use for any type of numeric value
      b. Come up with a concise description of what the column likely represents based on its name and values.
    3. If you are given 2 rows of data, treat first row as headers and use column names as names. If you are given a single row of data, generate names based on the values in the first row.
    4. Construct a JSON array with the following structure:
      ```json
      [
        {{
          ""name"": ""column1_name"",
          ""description"": ""column1_description"",
          ""type"": ""column1_type""
        }},
        {{
          ""name"": ""column2_name"", 
          ""description"": ""column2_description"",
          ""type"": ""column2_type""
        }},
        ...
      ]
      ```
    5. Output the complete JSON array.

    For example, if the input CSV looked like:

    ```
    name,age,city
    Alice,25,New York
    Bob,30,Chicago
    Charlie,35,Houston
    ```

    The expected JSON summary would be:
    [
      {{
        ""name"": ""name"",
        ""description"": ""The first name of the person"",
        ""type"": ""string""
      }},
      {{
        ""name"": ""age"",
        ""description"": ""The age of the person in years"",
        ""type"": ""integer""
      }},
      {{
        ""name"": ""city"",
        ""description"": ""The city where the person lives"",
        ""type"": ""string""
      }}
    ]

    Remember, ONLY output the JSON array. DO NOT provide any other output.
  """;
}