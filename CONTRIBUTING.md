# Contributing

This repository is specifically for Scroll Snaps for Unity. If you have a beautiful UI Component,
but it doesn't have to do with Scroll Snaps consider contributing to the [Unity UI Extensions](https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/overview) project.

##Creative Tenets

1. Components should be as self contained as possible, ease of installment for the user takes priority over minimizing duplicate code.
2. Components should not rely on vanilla unity components like Scroll Rects, they should handle whatever they need to do themselves, to make it easier for the user.
3. What you see is what you get, when the user is setting up the component in the editor it should look the same as how it will look when they run it.

##Pull Request Process

* Make sure the names of any new scripts match the standard used in other scripts.
* Put any new scripts in the **UnityEngine.UI.ScrollSnaps** namespace so that they do not interfear with other developments.
* Remove any unused "using *blank*" statements.
* Add a coment at the top of any new Component script that tells you it's dependencies & where to find them. E.g:

```
//Dependencies:
// - Scroller: Source > Scripts > HelperClasses
// - DirectionalScrollSnapEditor: Source > Editor (optional)
```

* Add **MenuItem** and **AddCompontentMenu** atributes to any new Components.
* Add/Update example projects under Source > Examples.
* Consider adding a custom editor to any new components (optional but awesome).
* Add yourself to the contributors file. Source > contributors.
* Put up your pull request.
* Wait for any feedback.
* Pull request will be merged by BeksOmega.