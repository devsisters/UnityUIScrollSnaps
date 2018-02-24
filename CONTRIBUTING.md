# Contributing

This repository is specifically for Scroll Snaps for Unity. If you have a beautiful UI Component,
but it doesn't have to do with Scroll Snaps consider contributing to the [Unity UI Extensions] (https://bitbucket.org/beksomega/unity-ui-extensions/overview) project.

##Pull Request Process

1. Make sure the names of any new scripts match the standard used in other scripts.
2. Put any new scripts in the **UnityEngine.UI.ScrollSnaps** namespace so that they do not interfear with other developments.
3. Remove any unused "using *blank*" statements.
4. Add a coment at the top of any new Component script that tells you it's dependencies & where to find them. E.g:

```
//Dependencies:
// - Scroller: Source > Scripts > HelperClasses
// - DirectionalScrollSnapEditor: Source > Editor (optional)
```

5. Add **MenuItem** and **AddCompontentMenu** atributes to any new Components.
6. Add/Update example projects under Source > Examples.
7. Consider adding a custom editor to any new components (optional but awesome).
8. Add yourself to the contributors file. Source > contributors.
9. Put up your pull request.
10. Wait for any feedback.
11. Pull request will be merged by BeksOmega.