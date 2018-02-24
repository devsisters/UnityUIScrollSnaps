# Contributing

This repository is specifically for Scroll Snaps for Unity. If you have a beautiful UI Component,
but it doesn't have to do with Scroll Snaps consider contributing to the [Unity UI Extensions] (https://bitbucket.org/beksomega/unity-ui-extensions/overview) project.

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