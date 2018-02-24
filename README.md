# Unity UI Scroll Snaps

This project is about extending the Unity UI system to create components that scroll and snap to items. The main creative tenents of this repository are: 

1. Components should be as self contained as possible, ease of installment for the user takes priority over minimizing duplicate code.
2. Components should not rely on vanilla unity components like Scroll Rects, they should handle whatever they need to do themselves, to make it easier for the user.
3. What you see is what you get, when the user is setting up the component in the editor it should look the same as how it will look when they run it.

## Getting Started

These instructions will make the Components in this project available to you inside of Unity, and allow you to get started making things.

### Download entire repository:

1.	Dowload: /Download link here/
2.	Open the Unity project you would like to import your newly downloaded assets into.
3.	On the top menu bar go to Assets > Import Package > Custom Package.
4.	Open the downloaded assets.
5.  Select Import on the new dialog window.

### Downloading specific components:

1. On Bitbucket go to Source > Scripts.
2. Find the script you would like to add to your project and open it.
3. At the top of that file it should say "Dependencies:" this will tell you if-there-are/what other scripts you need to download along with this one.
4. Download all chosen/needed scripts.
5. Open the Unity project you would like to import your newly downloaded assets into.
6. Drag your downloaded assets into the Project window in Unity.
7. Place any Editor scripts in a folder named "Editor", this keeps them from being added to builds, because they are not needed.

### Adding To Project

* Components can be added through the Hierarchy window through Create > UI > ScrollSnaps.
* Components can be added through the top menu bar by going to Gameobjects > UI > ScrollSnaps.
* You can add Components to all ready created gameobjects by:
	1. Selecting the gameobject you want to add the component to.
	2. In the Inspector window hitting the "Add Component" button and then either:
		* Searching for the component.
		* Going to UI > Scroll Snaps.
* You can add Components to all ready created gameobjects by:
	1. Selecting the gameobject you want to add the component to.
	2. Draggint the Component from the Project window into the Inspector window.

## Deployment

//What's deployment?

## Built With

//Do I need this?

* [Dropwizard](http://www.dropwizard.io/1.0.2/docs/) - The web framework used
* [Maven](https://maven.apache.org/) - Dependency Management
* [ROME](https://rometools.github.io/rome/) - Used to generate RSS Feeds

## Contributing

Please read [CONTRIBUTING.md](https://bitbucket.org/beksomega/unityuiscrollsnaps/src/master/CONTRIBUTING.md) for details on the process for submitting pull requests.

## Versioning

//Do I need this?

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 

## Authors

* **Beks_Omega** - *Initial work*

See also the list of [contributors](https://bitbucket.org/beksomega/unityuiscrollsnaps/src/master/contributors.txt) who participated in this project.

## License

//What license should I use?

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* Thank you to the Unity team for making the UI code available: [UI Repository](https://bitbucket.org/Unity-Technologies/ui/overview).