export class NavigationHelper {
    public static addBackHandler(backHandlerCallback: () => Boolean) {
        history.pushState(null, "", window.location.href);
        window.onpopstate = () => {
            const handled = backHandlerCallback();

            if (!handled) {
                history.back();
            } else {
                history.forward();
            }
        };
    }

    public static openUri(url?: string, target?: string): boolean {
        const w = window.open(url, target);
        return !!w;
    }
}
