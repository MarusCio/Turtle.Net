function toggleCommentForm(id) {
    document.querySelectorAll("[id^='commentForm-']").forEach(form => {
        if (form.id !== `commentForm-${id}`) {
            form.classList.add("d-none");
        }
    });

    const form = document.getElementById(`commentForm-${id}`);
    form.classList.toggle("d-none");
}

function showComments(id) {
    const ul = document.getElementById(`comments-of-${id}`);
    const arrow = document.getElementById(`arrow-${id}`);

    if (ul == null || arrow == null)
        return;
    
    ul.classList.toggle("d-none");
    if (arrow.classList.contains("bi-caret-down-fill")) {
        arrow.classList.remove("bi-caret-down-fill");
        arrow.classList.add("bi-caret-up-fill");


        let arr = sessionStorage.getItem("arrow-cache");

        if (arr == null) {
            arr = [id];
            sessionStorage.setItem("arrow-cache", JSON.stringify(arr));
        }
        else {
            let arr2 = JSON.parse(arr || "[]");
            arr2.push(id);
            sessionStorage.setItem("arrow-cache", JSON.stringify(arr2));
        }

        
    }
    else {
        arrow.classList.add("bi-caret-down-fill");
        arrow.classList.remove("bi-caret-up-fill");

        let arr = sessionStorage.getItem("arrow-cache");

        if (arr == null) return;
        else {
            let arr2 = JSON.parse(arr || "[]");
            arr2.splice(arr2.indexOf(id), 1);

            sessionStorage.setItem("arrow-cache", JSON.stringify(arr2));
        }
    }
}
window.onload = function () {
    var arrStr = sessionStorage.getItem("arrow-cache");
    sessionStorage.removeItem("arrow-cache");
    let arr = JSON.parse(arrStr || "[]");
    arr.forEach(showComments);
}