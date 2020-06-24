// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.6.2

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;

namespace CovidBot.Dialogs
{
    public class UserDetailsDialog : CancelAndHelpDialog
    {
        public UserProfile user = new UserProfile();
        public UserDetailsDialog(CovidRecognizer luisRecognizer)
                 : base(nameof(UserDetailsDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NameStepAsync,
                AgeStepAsync,
                PincodeStepAsync,
                AddressStepAsync,
                ContactStepAsync,
                FinalStepAsync,
            }));
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string ask = "sorry, your are tested postive for Covid, Please take the responsibility of providing your details to help us trace and alert your surroundings. \n Your name please:";
            var promptMessage = MessageFactory.Text(ask, ask, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }
        private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            user.Name = stepContext.Context.Activity.Text;
            string ask = "Age";
            var promptMessage = MessageFactory.Text(ask, ask, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }
        private async Task<DialogTurnResult> PincodeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var r = (UserProfile)stepContext.Options;
            int i = 0;
            Int32.TryParse(stepContext.Context.Activity.Text,out i);
            user.Age = i;
            string ask = "Pincode";
            var promptMessage = MessageFactory.Text(ask, ask, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }


        private async Task<DialogTurnResult> AddressStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            int i = 0;
            Int32.TryParse(stepContext.Context.Activity.Text, out i);
            user.Pincode = i;
            string ask = "Address";
            var promptMessage = MessageFactory.Text(ask, ask, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ContactStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            user.Address = stepContext.Context.Activity.Text;
            string ask = "Contact number";
            var promptMessage = MessageFactory.Text(ask, ask, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            user.Mobile = stepContext.Context.Activity.Text;
            using (HttpClient client = new HttpClient())
            {
                {
                    string url = string.Concat("https://prod-02.eastus.logic.azure.com/workflows/84602e3e5e8c4b8695b56ed9c19121be/versions/08586113857863966974/triggers/manual/paths/invoke/email?api-version=2016-10-01&sp=%2Fversions%2F08586113857863966974%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=tzv7Ll12-2OPIqdqJKuQeeh2rX8jNDIxwkda-5CGk4U");
                    var stringContent = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                    var httpResponse = await client.PostAsync(url, stringContent);
                }
            }
                return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
