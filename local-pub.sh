#!/bin/bash
sudo dotnet publish -o /home/rg/pub -c Release && sudo cp appsettings.* ~rg/pub/ && sudo chown -R rg ~rg/pub