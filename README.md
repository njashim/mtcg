# mtcg - MonsterTradingCardGame
by Nahi Islam Jashim | for my semester project in swen

GitHub-Repository: https://github.com/njashim/mtcg

## design

I didn't really use any design pattern or something like that. I just started coding and tried to keep my code simple as possible. First I started with the code of my Server-Client without enabling Multi-Threading. Then I wrote the code for the Response- & Request-Handler.
I designed my `ResponseHandler.cs` class so that I have a dictionary with the status codes & their description and a method `string GetResponseMessage(int statusCode, string contentType, string customMessage)`. I give my method the respective parameters and get my response message back.
My `RequestHandler.cs` is constructed so that it gets the request and the Database object from the `Program.cs` which is responsible for Server-Client and then searches for the respective curl command in my if-else statements.
And the last thing is my `Database.cs` class which is responsible for everything related to the database. For example database connection, creating anything, deleting anything, updating anything.

## lessons learned

My biggest mistake was that I didn't really understand the specification and that I didn't write the unit tests before/while programming the code. I think I would have been much faster if I understood the specifications and did test-driven-development. 
New things for me were the connection between C# and PostgreSQL, the use of postgresql related methods, Json-Serializing and the use of JsonIgnore attributes.

## unit testing decisions

Tbh I didn't really made a crazy decision. I just simply tested the getter and setter of my classes `User.cs`, `Card.cs`, `Trading.cs`. In my opinion register and login are the key functions. That's why I tested in my `Database.cs` class my first 5-7 methods. These tests include the process of registering and logging in a user and also creating packs and cards. One thing I have to say is that I used the AAA principle for the unit tests.

## unique feature

daily login bonus

* every 24 hours the user can retrieve a daily login bonus over this route 
  `POST http://localhost:10001/daily-login`
* the user starts with an 1d streak, which gets higher by retrieving the daily login bonus after 24 hours
* after each day the reward of the daily login set back to 1d
* in the code I used 1min instead of 24hours and 2min instead of 48hours to show my working unique feature to the teacher, but I still wrote down the code for 24 hours as a comment

## tracked time

in total I worked 68 hours on this project
