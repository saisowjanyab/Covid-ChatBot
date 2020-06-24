// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.6.2

using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CovidBot.CognitiveModels;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;

namespace CovidBot.Dialogs
{
    public class DiagnoseDialog : CancelAndHelpDialog
    {

        private static string attachmentPromptId = $"{nameof(DiagnoseDialog)}_attachmentPrompt";
        private readonly CovidRecognizer _luisRecognizer;

        public DiagnoseDialog(CovidRecognizer luisRecognizer,UserDetailsDialog userDetailsDialog)
            : base(nameof(DiagnoseDialog))
        {
            _luisRecognizer = luisRecognizer;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new AttachmentPrompt(attachmentPromptId));
            AddDialog(userDetailsDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AskSymptomsStepAsync,
                GetSymptomsStepAsync,
                XRayStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AskSymptomsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = "Please mention some of the symptoms you have?";
            var messageTextE = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = messageTextE }, cancellationToken);
        }

        private async Task<DialogTurnResult> GetSymptomsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = await _luisRecognizer.RecognizeAsync<CovidHealthBot>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case CovidHealthBot.Intent.Symptoms_At_Risk:
                    if (luisResult.Entities.NonSymptoms != null && luisResult.Entities.symptoms !=null)
                    {
                        if (luisResult.Entities.symptoms.Length > luisResult.Entities.NonSymptoms.Length)
                        {
                            var messageText = "Sorry you might be at risk, do you want to continue with furthur diagnosis?";
                            var messageTextE = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = messageTextE }, cancellationToken);
                        }
                        else
                        {
                            var messageText = "Your symptoms doesnot match covid, Be careful Maintain social distance";
                            var messageTextE = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                            var c = await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = messageTextE }, cancellationToken);
                            return await stepContext.EndDialogAsync(null, cancellationToken);
                        }
                    }
                    if(luisResult.Entities.symptoms == null)
                    {
                        var messageText = "Your symptoms doesnot match covid, Be careful Maintain social distance";
                        var messageTextE = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                        var c = await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = messageTextE }, cancellationToken);
                        return await stepContext.EndDialogAsync(null, cancellationToken);
                    }
                    if(luisResult.Entities.NonSymptoms == null)
                    {
                        var messageText = "Sorry you might be at risk, do you want to continue with furthur diagnosis?";
                        var messageTextE = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = messageTextE }, cancellationToken);
                    }
                    break;
                default:
                    return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> XRayStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var messageText = "Please attach image of your X-Ray in jpeg/png format";
                var messageTextE = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                return await stepContext.PromptAsync(attachmentPromptId, new PromptOptions { Prompt = messageTextE }, cancellationToken);
            }
            else
            {
                var messageText = "Be careful Maintain social distance";
                var messageTextE = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                var c = await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = messageTextE }, cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            List<Attachment> attachments = (List<Attachment>)stepContext.Result;
            dynamic covidResult = new ExpandoObject();
            foreach (var file in attachments)
            {
                var remoteFileUrl = file.ContentUrl;
                var localFileName = Path.Combine(Path.GetTempPath(), file.Name);
                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(remoteFileUrl, localFileName);
                }
                byte[] fileContents = File.ReadAllBytes(localFileName);
                MultipartFormDataContent form = new MultipartFormDataContent
                        {
                             { new ByteArrayContent(fileContents, 0, fileContents.Length), "file", "pic.jpeg" }
                         };
                HttpClient client = new HttpClient();
                var waitingText = "Please wait as our AI processing your request";
                var waitingTextE = MessageFactory.Text(waitingText, waitingText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(waitingTextE, cancellationToken);
                var response =  await client.PostAsync("http://ids.pythonanywhere.com/predict", form);
                var s = await response.Content.ReadAsStringAsync();
                covidResult = JsonConvert.DeserializeObject(s);
            }
            if (covidResult.result == "covid")
            {
                return await stepContext.BeginDialogAsync(nameof(UserDetailsDialog), null, cancellationToken);
            }
            else
            {
                var messageText = "You are tested negative; However please be careful";
                var messageTextE = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(messageTextE, cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = "Be careful Maintain social distance and update us";
            var messageTextE = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
            await stepContext.Context.SendActivityAsync(messageTextE, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
